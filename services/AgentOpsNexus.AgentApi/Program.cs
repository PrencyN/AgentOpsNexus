using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Data;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

/*
 * Validate agent configuration at application startup.
 *
 * Mock:
 * - Does not require Foundry settings.
 *
 * Foundry:
 * - Requires a valid project endpoint.
 * - Requires a deployed model name.
 * - Requires a Microsoft Entra tenant ID.
 */
builder.Services
    .AddOptions<AgentOptions>()
    .Bind(
        builder.Configuration.GetSection(
            AgentOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options =>
        {
            var provider =
                options.Provider.Trim();

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
        "Foundry requires ProjectEndpoint, " +
        "ModelDeployment, and TenantId.")
    .ValidateOnStart();

/*
 * Return consistent validation errors through
 * ASP.NET Core ProblemDetails.
 */
builder.Services.Configure<ApiBehaviorOptions>(
    options =>
    {
        options.InvalidModelStateResponseFactory =
            context =>
            {
                var problem =
                    new ValidationProblemDetails(
                        context.ModelState)
                    {
                        Status =
                            StatusCodes.Status400BadRequest,
                        Title =
                            "Request validation failed",
                        Detail =
                            "One or more request fields " +
                            "are invalid.",
                        Type =
                            "https://agentops-nexus/errors/" +
                            "request-validation-failed"
                    };

                problem.Extensions["code"] =
                    "REQUEST_VALIDATION_FAILED";

                problem.Extensions["retryable"] =
                    false;

                return new BadRequestObjectResult(
                    problem);
            };
    });

/*
 * SQLite persistence configuration.
 *
 * appsettings.json provides the normal local connection
 * string. The fallback protects startup if that section
 * is accidentally missing.
 */
var connectionString =
    builder.Configuration.GetConnectionString(
        "AgentOpsDatabase")
    ??
    "Data Source=App_Data/agentops.db";

/*
 * Ensure the SQLite database directory exists before
 * Entity Framework opens the connection.
 */
var databasePath =
    Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data");

Directory.CreateDirectory(databasePath);

builder.Services.AddDbContext<AgentOpsDbContext>(
    options =>
    {
        options.UseSqlite(connectionString);

        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
        }

        /*
         * Do not enable sensitive-data logging here.
         *
         * Incident prompts and operational information
         * must not be copied into EF diagnostic logs.
         */
    });

/*
 * DbContext is scoped, so the repository must also
 * be scoped.
 */
builder.Services.AddScoped<
    IAgentRequestRepository,
    EfAgentRequestRepository>();

/*
 * Deterministic read-only deployment evidence provider.
 */
builder.Services.AddSingleton<
    IDeploymentEvidenceService,
    MockDeploymentEvidenceService>();

/*
 * Agent Framework function-tool container.
 */
builder.Services.AddSingleton<DeploymentTools>();

/*
 * Register both agent implementations.
 *
 * The configured provider determines which implementation
 * is exposed through IIncidentAgent.
 */
builder.Services.AddSingleton<MockIncidentAgent>();
builder.Services.AddSingleton<FoundryIncidentAgent>();

builder.Services.AddSingleton<IIncidentAgent>(
    serviceProvider =>
    {
        var options =
            serviceProvider
                .GetRequiredService<
                    IOptions<AgentOptions>>()
                .Value;

        if (string.Equals(
                options.Provider,
                "Foundry",
                StringComparison.OrdinalIgnoreCase))
        {
            return serviceProvider
                .GetRequiredService<
                    FoundryIncidentAgent>();
        }

        return serviceProvider
            .GetRequiredService<
                MockIncidentAgent>();
    });

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Frontend",
            policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173")
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
    (
        IOptions<AgentOptions> options,
        AgentOpsDbContext dbContext) =>
    {
        var usingFoundry =
            string.Equals(
                options.Value.Provider,
                "Foundry",
                StringComparison.OrdinalIgnoreCase);

        return Results.Ok(
            new
            {
                service =
                    "AgentOpsNexus.AgentApi",
                status =
                    "Running",
                agentProvider =
                    options.Value.Provider,
                modelDeployment =
                    usingFoundry
                        ? options.Value.ModelDeployment
                        : null,
                groundedDeploymentTool =
                    usingFoundry,
                deploymentEvidenceProvider =
                    "MockDeploymentEvidenceService",
                persistenceProvider =
                    dbContext.Database.ProviderName,
                frontend =
                    "http://localhost:5173",
                health =
                    "/health",
                requestsApi =
                    "/api/v1/requests"
            });
    });

app.MapGet(
    "/health",
    async (
        IOptions<AgentOptions> options,
        AgentOpsDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var databaseAvailable =
            await dbContext.Database
                .CanConnectAsync(cancellationToken);

        var status =
            databaseAvailable
                ? "Healthy"
                : "Degraded";

        return databaseAvailable
            ? Results.Ok(
                new
                {
                    status,
                    service =
                        "AgentOpsNexus.AgentApi",
                    agentProvider =
                        options.Value.Provider,
                    deploymentTool =
                        "GetRecentDeployments",
                    persistence =
                        "SQLite",
                    databaseAvailable,
                    timestamp =
                        DateTimeOffset.UtcNow
                })
            : Results.Json(
                new
                {
                    status,
                    service =
                        "AgentOpsNexus.AgentApi",
                    agentProvider =
                        options.Value.Provider,
                    persistence =
                        "SQLite",
                    databaseAvailable,
                    timestamp =
                        DateTimeOffset.UtcNow
                },
                statusCode:
                    StatusCodes
                        .Status503ServiceUnavailable);
    });

/*
 * Apply checked-in EF Core migrations during startup.
 *
 * This is convenient for local development and the
 * capstone demonstration. In a production deployment,
 * run migrations as a controlled deployment step rather
 * than from every application instance.
 */
using (var scope =
       app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                AgentOpsDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.Run();

public partial class Program;