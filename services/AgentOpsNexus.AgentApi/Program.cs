using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services
    .AddOptions<AgentOptions>()
    .Bind(
        builder.Configuration.GetSection(
            AgentOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
        {
            var provider = options.Provider.Trim();

            if (string.Equals(
                    provider,
                    "Mock",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.Equals(
                    provider,
                    "Foundry",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return
                Uri.TryCreate(
                    options.ProjectEndpoint,
                    UriKind.Absolute,
                    out _)
                &&
                !string.IsNullOrWhiteSpace(
                    options.ModelDeployment)
                &&
                !string.IsNullOrWhiteSpace(
                    options.TenantId);
        },
        "Agent provider must be Mock or Foundry. " +
        "Foundry requires ProjectEndpoint, ModelDeployment, and TenantId.")
    .ValidateOnStart();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem =
            new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed",
                Detail = "One or more request fields are invalid.",
                Type =
                    "https://agentops-nexus/errors/" +
                    "request-validation-failed"
            };

        problem.Extensions["code"] =
            "REQUEST_VALIDATION_FAILED";

        problem.Extensions["retryable"] = false;

        return new BadRequestObjectResult(problem);
    };
});

/*
 * Request-history storage.
 *
 * This remains in memory for the current milestone.
 */
builder.Services.AddSingleton<
    IAgentRequestRepository,
    InMemoryAgentRequestRepository>();

/*
 * Deployment evidence provider.
 *
 * The mock implementation returns deterministic demonstration records.
 * A future implementation can replace this with an Azure DevOps or GitHub
 * deployment adapter without changing DeploymentTools.
 */
builder.Services.AddSingleton<
    IDeploymentEvidenceService,
    MockDeploymentEvidenceService>();

/*
 * Read-only Agent Framework tool.
 */
builder.Services.AddSingleton<DeploymentTools>();

/*
 * Both agent implementations remain registered.
 *
 * AgentOptions.Provider controls which one is exposed as IIncidentAgent.
 */
builder.Services.AddSingleton<MockIncidentAgent>();
builder.Services.AddSingleton<FoundryIncidentAgent>();

builder.Services.AddSingleton<IIncidentAgent>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<AgentOptions>>()
            .Value;

        if (string.Equals(
                options.Provider,
                "Foundry",
                StringComparison.OrdinalIgnoreCase))
        {
            return serviceProvider
                .GetRequiredService<FoundryIncidentAgent>();
        }

        return serviceProvider
            .GetRequiredService<MockIncidentAgent>();
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");

app.MapControllers();

app.MapGet(
    "/",
    (IOptions<AgentOptions> options) =>
    {
        var usingFoundry =
            string.Equals(
                options.Value.Provider,
                "Foundry",
                StringComparison.OrdinalIgnoreCase);

        return Results.Ok(
            new
            {
                service = "AgentOpsNexus.AgentApi",
                status = "Running",
                agentProvider = options.Value.Provider,
                modelDeployment =
                    usingFoundry
                        ? options.Value.ModelDeployment
                        : null,
                groundedDeploymentTool = usingFoundry,
                deploymentEvidenceProvider =
                    "MockDeploymentEvidenceService",
                frontend = "http://localhost:5173",
                health = "/health",
                requestsApi = "/api/v1/requests"
            });
    });

app.MapGet(
    "/health",
    (IOptions<AgentOptions> options) =>
    {
        return Results.Ok(
            new
            {
                status = "Healthy",
                service = "AgentOpsNexus.AgentApi",
                agentProvider = options.Value.Provider,
                deploymentTool = "GetRecentDeployments",
                timestamp = DateTimeOffset.UtcNow
            });
    });

app.Run();

public partial class Program;