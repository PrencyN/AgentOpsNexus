using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Data;
using AgentOpsNexus.AgentApi.Middleware;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

const string serviceName =
    "AgentOpsNexus.AgentApi";

var serviceVersion =
    typeof(Program)
        .Assembly
        .GetName()
        .Version?
        .ToString()
    ??
    "unknown";

/*
 * ASP.NET Core infrastructure.
 */
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

/*
 * Validate agent configuration during startup.
 *
 * Mock:
 * - Foundry settings are not required.
 *
 * Foundry:
 * - ProjectEndpoint must be a valid absolute URI.
 * - ModelDeployment must be configured.
 * - TenantId must be configured.
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
 * Produce consistent validation failures using
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
                            StatusCodes
                                .Status400BadRequest,

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

                problem.Extensions["correlationId"] =
                    context.HttpContext
                        .TraceIdentifier;

                return new BadRequestObjectResult(
                    problem);
            };
    });

/*
 * SQLite persistence.
 */
var connectionString =
    builder.Configuration.GetConnectionString(
        "AgentOpsDatabase")
    ??
    "Data Source=App_Data/agentops.db";

/*
 * Ensure the database directory exists before
 * SQLite opens the database file.
 */
var databaseDirectory =
    Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data");

Directory.CreateDirectory(
    databaseDirectory);

builder.Services.AddDbContext<
    AgentOpsDbContext>(
        options =>
        {
            options.UseSqlite(
                connectionString);

            if (builder.Environment
                .IsDevelopment())
            {
                options.EnableDetailedErrors();
            }

            /*
             * Do not enable sensitive-data logging.
             *
             * Incident prompts, model responses,
             * credentials, endpoints, and tenant IDs
             * must not be written to EF diagnostics.
             */
        });

/*
 * DbContext is scoped, so its repository must also
 * be scoped.
 */
builder.Services.AddScoped<
    IAgentRequestRepository,
    EfAgentRequestRepository>();

/*
 * Deterministic read-only deployment evidence.
 */
builder.Services.AddSingleton<
    IDeploymentEvidenceService,
    MockDeploymentEvidenceService>();

/*
 * Agent Framework function-tool container.
 */
builder.Services.AddSingleton<
    DeploymentTools>();

/*
 * Custom metrics service.
 *
 * IMeterFactory is supplied by the .NET dependency
 * injection container.
 */
builder.Services.AddSingleton<
    AgentOpsMetrics>();

/*
 * Both agent implementations remain registered.
 *
 * AgentOptions.Provider determines which implementation
 * is exposed as IIncidentAgent.
 */
builder.Services.AddSingleton<
    MockIncidentAgent>();

builder.Services.AddSingleton<
    FoundryIncidentAgent>();

builder.Services.AddSingleton<
    IIncidentAgent>(
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

/*
 * OpenTelemetry resource, tracing, and metrics.
 */
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(
        resource =>
            resource.AddService(
                serviceName: serviceName,
                serviceVersion:
                    serviceVersion))
    .WithTracing(
        tracing =>
        {
            tracing
                .AddSource(
                    AgentOpsActivitySource
                        .SourceName)
                .AddAspNetCoreInstrumentation(
                    options =>
                    {
                        /*
                         * Health probes can be excluded from
                         * distributed traces to reduce noise.
                         */
                        options.Filter =
                            httpContext =>
                                !httpContext.Request.Path
                                    .StartsWithSegments(
                                        "/health");
                    })
                .AddHttpClientInstrumentation();

            if (builder.Environment
                .IsDevelopment())
            {
                tracing.AddConsoleExporter();
            }
        })
    .WithMetrics(
        metrics =>
        {
            metrics
                .AddMeter(
                    AgentOpsMetrics.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (builder.Environment
                .IsDevelopment())
            {
                metrics.AddConsoleExporter();
            }
        });

/*
 * Local React development origin.
 */
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
                    .AllowAnyMethod()
                    .WithExposedHeaders(
                        CorrelationIdMiddleware
                            .HeaderName);
            });
    });

var app = builder.Build();

/*
 * Central exception handling must run near the
 * beginning of the pipeline.
 */
app.UseExceptionHandler();

/*
 * Generate or preserve X-Correlation-ID and make it
 * available to controllers, logs, and traces.
 */
app.UseMiddleware<
    CorrelationIdMiddleware>();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

/*
 * API service summary.
 */
app.MapGet(
    "/",
    (
        IOptions<AgentOptions> options,
        AgentOpsDbContext dbContext,
        HttpContext httpContext) =>
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
                    serviceName,

                version =
                    serviceVersion,

                status =
                    "Running",

                correlationId =
                    httpContext.TraceIdentifier,

                agentProvider =
                    options.Value.Provider,

                modelDeployment =
                    usingFoundry
                        ? options.Value
                            .ModelDeployment
                        : null,

                groundedDeploymentTool =
                    usingFoundry,

                deploymentEvidenceProvider =
                    "MockDeploymentEvidenceService",

                persistenceProvider =
                    dbContext.Database
                        .ProviderName,

                telemetry =
                    "OpenTelemetry",

                frontend =
                    "http://localhost:5173",

                health =
                    "/health",

                requestsApi =
                    "/api/v1/requests"
            });
    });

/*
 * Health check including database connectivity.
 */
app.MapGet(
    "/health",
    async (
        IOptions<AgentOptions> options,
        AgentOpsDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var databaseAvailable =
            await dbContext.Database
                .CanConnectAsync(
                    cancellationToken);

        var status =
            databaseAvailable
                ? "Healthy"
                : "Degraded";

        var response =
            new
            {
                status,

                service =
                    serviceName,

                version =
                    serviceVersion,

                correlationId =
                    httpContext.TraceIdentifier,

                agentProvider =
                    options.Value.Provider,

                deploymentTool =
                    "GetRecentDeployments",

                persistence =
                    "SQLite",

                databaseAvailable,

                telemetry =
                    "OpenTelemetry",

                timestamp =
                    DateTimeOffset.UtcNow
            };

        if (databaseAvailable)
        {
            return Results.Ok(response);
        }

        return Results.Json(
            response,
            statusCode:
                StatusCodes
                    .Status503ServiceUnavailable);
    });

/*
 * Apply checked-in migrations at startup.
 *
 * This is convenient for local development and the
 * capstone demonstration. For production, run database
 * migrations as a controlled deployment operation
 * before starting application instances.
 */
using (var migrationScope =
       app.Services.CreateScope())
{
    var dbContext =
        migrationScope
            .ServiceProvider
            .GetRequiredService<
                AgentOpsDbContext>();

    await dbContext.Database
        .MigrateAsync();
}

app.Run();

public partial class Program;