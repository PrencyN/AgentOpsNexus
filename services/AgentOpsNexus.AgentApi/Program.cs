using System.Threading.RateLimiting;
using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Data;
using AgentOpsNexus.AgentApi.Evaluation;
using AgentOpsNexus.AgentApi.Middleware;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Security;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
 * ASP.NET Core foundation.
 */
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

/*
 * Agent configuration validation.
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
 * Consistent validation errors.
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
             * Sensitive-data logging is deliberately
             * disabled. Prompts, responses, secrets,
             * endpoints, and tenant IDs must not appear
             * in EF diagnostic logs.
             */
        });

builder.Services.AddScoped<
    IAgentRequestRepository,
    EfAgentRequestRepository>();

/*
 * Security and deterministic evaluation services.
 */
builder.Services.AddSingleton<
    IncidentInputPolicy>();

builder.Services.AddSingleton<
    IncidentResponseEvaluator>();

/*
 * Grounded deployment evidence.
 */
builder.Services.AddSingleton<
    IDeploymentEvidenceService,
    MockDeploymentEvidenceService>();

builder.Services.AddSingleton<
    DeploymentTools>();

/*
 * OpenTelemetry custom metrics.
 */
builder.Services.AddSingleton<
    AgentOpsMetrics>();

/*
 * Agent implementations.
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
 * OpenTelemetry tracing and metrics.
 */
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(
        resource =>
            resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion))
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
                         * Health probes are excluded from
                         * traces to reduce observability
                         * noise.
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
 * Rate limiting.
 *
 * Create policy:
 * - 20 accepted request creations per minute per client.
 *
 * Stream policy:
 * - Maximum 5 concurrent stream requests per client.
 */
builder.Services.AddRateLimiter(
    options =>
    {
        options.RejectionStatusCode =
            StatusCodes
                .Status429TooManyRequests;

        options.AddPolicy(
            "investigation-create",
            httpContext =>
            {
                var partitionKey =
                    GetClientPartitionKey(
                        httpContext);

                return RateLimitPartition
                    .GetFixedWindowLimiter(
                        partitionKey,
                        _ =>
                            new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,

                                Window =
                                    TimeSpan
                                        .FromMinutes(1),

                                QueueLimit = 0,

                                AutoReplenishment =
                                    true
                            });
            });

        options.AddPolicy(
            "investigation-stream",
            httpContext =>
            {
                var partitionKey =
                    GetClientPartitionKey(
                        httpContext);

                return RateLimitPartition
                    .GetConcurrencyLimiter(
                        partitionKey,
                        _ =>
                            new ConcurrencyLimiterOptions
                            {
                                PermitLimit = 5,

                                QueueLimit = 0
                            });
            });

        options.OnRejected =
            async (
                context,
                cancellationToken) =>
            {
                var response =
                    context.HttpContext.Response;

                response.StatusCode =
                    StatusCodes
                        .Status429TooManyRequests;

                response.ContentType =
                    "application/problem+json";

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out var retryAfter))
                {
                    response.Headers.RetryAfter =
                        Math.Max(
                            1,
                            (int)Math.Ceiling(
                                retryAfter
                                    .TotalSeconds))
                        .ToString();
                }

                var problem =
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes
                                .Status429TooManyRequests,

                        Title =
                            "Request rate limit exceeded",

                        Detail =
                            "Too many investigation requests " +
                            "are active or were submitted in " +
                            "the current time window. Retry " +
                            "after the limit resets.",

                        Type =
                            "https://agentops-nexus/errors/" +
                            "rate-limit-exceeded"
                    };

                problem.Extensions["code"] =
                    "RATE_LIMIT_EXCEEDED";

                problem.Extensions["retryable"] =
                    true;

                problem.Extensions["correlationId"] =
                    context.HttpContext
                        .TraceIdentifier;

                await response.WriteAsJsonAsync(
                    problem,
                    cancellationToken);
            };
    });

/*
 * React development origin.
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
                            .HeaderName,
                        "Retry-After");
            });
    });

var app = builder.Build();

/*
 * Middleware pipeline.
 *
 * Correlation ID runs before security, rate limiting,
 * endpoint logic, and exception handling responses.
 */
app.UseExceptionHandler();

app.UseMiddleware<
    CorrelationIdMiddleware>();

app.UseMiddleware<
    SecurityHeadersMiddleware>();

app.UseCors("Frontend");

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

/*
 * API summary.
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

                security =
                    new
                    {
                        inputPolicy =
                            "enabled",

                        securityHeaders =
                            "enabled",

                        rateLimiting =
                            "enabled"
                    },

                frontend =
                    "http://localhost:5173",

                health =
                    "/health",

                requestsApi =
                    "/api/v1/requests"
            });
    });

/*
 * Health endpoint is intentionally not rate-limited.
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

                security =
                    "Enabled",

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
 * Apply checked-in EF Core migrations.
 *
 * Production deployments should run migrations as a
 * controlled deployment step rather than from every
 * application process.
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

static string GetClientPartitionKey(
    HttpContext httpContext)
{
    /*
     * Prefer the authenticated identity when available.
     */
    var userName =
        httpContext.User.Identity?.Name;

    if (!string.IsNullOrWhiteSpace(userName))
    {
        return $"user:{userName}";
    }

    /*
     * For local development, this normally resolves to
     * the loopback address.
     *
     * In production behind a trusted reverse proxy,
     * configure ForwardedHeaders before relying on the
     * remote IP address.
     */
    var remoteAddress =
        httpContext.Connection
            .RemoteIpAddress?
            .ToString();

    if (!string.IsNullOrWhiteSpace(
            remoteAddress))
    {
        return $"ip:{remoteAddress}";
    }

    return "client:unknown";
}

/*
 * Keep this declaration at the absolute end of the file.
 * It enables WebApplicationFactory-based integration tests.
 */
public partial class Program;