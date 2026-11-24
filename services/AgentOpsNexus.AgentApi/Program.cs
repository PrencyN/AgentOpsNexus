using System.Threading.RateLimiting;
using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Data;
using AgentOpsNexus.AgentApi.Evaluation;
using AgentOpsNexus.AgentApi.Health;
using AgentOpsNexus.AgentApi.Middleware;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Security;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder =
    WebApplication.CreateBuilder(args);

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
 * Incoming API authentication and role-based
 * authorization.
 *
 * When Authentication:Enabled is true, the API
 * validates Microsoft Entra access tokens through
 * Microsoft.Identity.Web.
 *
 * When disabled, Development and Testing use the
 * deterministic local authentication scheme.
 *
 * Other environments cannot disable authentication.
 */
builder.Services
    .AddAgentOpsAuthentication(
        builder.Configuration,
        builder.Environment);

/*
 * Authenticated user and ownership resolution.
 */
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentUserAccessor,
    HttpContextCurrentUserAccessor>();

/*
 * Production readiness configuration.
 */
builder.Services
    .AddOptions<ProductionOptions>()
    .Bind(
        builder.Configuration.GetSection(
            ProductionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<
    IValidateOptions<ProductionOptions>,
    ProductionOptionsValidator>();


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
                            string.Concat(
                                "https",
                                "://",
                                "agentops-nexus",
                                "/errors/",
                                "request-validation-failed")
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
 * Persistence configuration.
 *
 * Development and Testing use SQLite.
 * Production uses SQL Server.
 */
builder.Services
    .AddOptions<PersistenceOptions>()
    .Bind(
        builder.Configuration.GetSection(
            PersistenceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<
    IValidateOptions<PersistenceOptions>,
    PersistenceOptionsValidator>();

var persistenceOptions =
    builder.Configuration
        .GetSection(
            PersistenceOptions.SectionName)
        .Get<PersistenceOptions>()
    ??
    new PersistenceOptions();

var connectionString =
    builder.Configuration.GetConnectionString(
        persistenceOptions
            .ConnectionStringName);

if (string.IsNullOrWhiteSpace(
        connectionString))
{
    throw new InvalidOperationException(
        "Persistence connection string " +
        $"'{persistenceOptions.ConnectionStringName}' " +
        "is not configured.");
}

if (persistenceOptions.IsSqlite)
{
    var databaseDirectory =
        Path.Combine(
            builder.Environment.ContentRootPath,
            "App_Data");

    Directory.CreateDirectory(
        databaseDirectory);
}

builder.Services.AddDbContext<
    AgentOpsDbContext>(
        options =>
        {
            if (persistenceOptions.IsSqlite)
            {
                options.UseSqlite(
                    connectionString);
            }
            else if (persistenceOptions.IsSqlServer)
            {
                options.UseSqlServer(
                    connectionString,
                    sqlServerOptions =>
                    {
                        sqlServerOptions
                            .EnableRetryOnFailure(
                                maxRetryCount:
                                    5,

                                maxRetryDelay:
                                    TimeSpan
                                        .FromSeconds(30),

                                errorNumbersToAdd:
                                    null);
                    });
            }
            else
            {
                throw new InvalidOperationException(
                    "Unsupported persistence provider " +
                    $"'{persistenceOptions.Provider}'.");
            }

            if (builder.Environment
                .IsDevelopment())
            {
                options.EnableDetailedErrors();
            }

            /*
             * Sensitive-data logging remains disabled.
             * Prompts, responses, identities, secrets,
             * endpoints, and connection strings must not
             * appear in EF diagnostic logs.
             */
        });

/*
 * Health checks.
 *
 * Liveness selects no dependency checks.
 * Readiness selects checks tagged "ready".
 */
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseReadinessCheck>(
        name:
            "agentops-database",

        failureStatus:
            Microsoft.Extensions
                .Diagnostics
                .HealthChecks
                .HealthStatus
                .Unhealthy,

        tags:
            new[]
            {
                "ready"
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
                serviceName:
                    serviceName,
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
 * - 20 accepted request creations per minute
 *   per authenticated user or client.
 *
 * Stream policy:
 * - Maximum 5 concurrent stream requests
 *   per authenticated user or client.
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
                                PermitLimit =
                                    20,

                                Window =
                                    TimeSpan
                                        .FromMinutes(1),

                                QueueLimit =
                                    0,

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
                                PermitLimit =
                                    5,

                                QueueLimit =
                                    0
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
                            string.Concat(
                                "https",
                                "://",
                                "agentops-nexus",
                                "/errors/",
                                "rate-limit-exceeded")
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
 * Frontend origins are environment-specific.
 *
 * Development configures localhost.
 * Production must explicitly configure every
 * allowed SPA origin.
 */
var productionOptions =
    builder.Configuration
        .GetSection(
            ProductionOptions.SectionName)
        .Get<ProductionOptions>()
    ??
    new ProductionOptions();

var allowedFrontendOrigins =
    productionOptions.AllowedOrigins
        .Where(
            origin =>
                !string.IsNullOrWhiteSpace(
                    origin))
        .Select(
            origin =>
                origin.Trim()
                    .TrimEnd('/'))
        .Distinct(
            StringComparer.OrdinalIgnoreCase)
        .ToArray();

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy(
            "Frontend",
            policy =>
            {
                if (
                    allowedFrontendOrigins
                        .Length > 0)
                {
                    policy.WithOrigins(
                        allowedFrontendOrigins);
                }
                else
                {
                    /*
                     * No origins are allowed when none
                     * are configured. Production
                     * validation prevents this state.
                     */
                    policy.SetIsOriginAllowed(
                        _ =>
                            false);
                }

                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(
                        CorrelationIdMiddleware
                            .HeaderName,
                        "Retry-After");
            });
    });

var app =
    builder.Build();

/*
 * Middleware pipeline.
 *
 * Correlation ID runs before authentication,
 * authorization, rate limiting, endpoint logic,
 * and exception-handler responses.
 */
app.UseExceptionHandler();

app.UseMiddleware<
    CorrelationIdMiddleware>();

app.UseMiddleware<
    SecurityHeadersMiddleware>();

app.UseCors("Frontend");

/*
 * Authentication establishes HttpContext.User.
 * Authorization evaluates endpoint policies.
 */
app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

/*
 * API summary.
 *
 * The summary endpoint remains anonymous so local
 * tooling can inspect service capabilities without
 * requesting an access token.
 */
app.MapGet(
        "/",
        (
            IOptions<AgentOptions> options,
            IOptions<
                AuthenticationOptions>
                authenticationOptions,
            AgentOpsDbContext dbContext,
            HttpContext httpContext) =>
        {
            var usingFoundry =
                string.Equals(
                    options.Value.Provider,
                    "Foundry",
                    StringComparison.OrdinalIgnoreCase);

            var authenticationMode =
                authenticationOptions
                    .Value
                    .Enabled
                    ? "Microsoft Entra ID"
                    : "Development";

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

                    authentication =
                        authenticationMode,

                    security =
                        new
                        {
                            inputPolicy =
                                "enabled",

                            securityHeaders =
                                "enabled",

                            rateLimiting =
                                "enabled",

                            authentication =
                                "enabled",

                            authorization =
                                "enabled"
                        },

                    frontendOrigins  =
                        allowedFrontendOrigins,

                    health =
                        new
                        {
                            live =
                                "/health/live",

                            ready =
                                "/health/ready",

                            legacy =
                                "/health"
                        },

                    requestsApi =
                        "/api/v1/requests"
                });
        })
    .AllowAnonymous();

/*
 * Liveness proves only that the application process
 * is running. It deliberately performs no database
 * or external dependency checks.
 */
app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate =
                _ =>
                    false,

            ResponseWriter =
                HealthResponseWriter
                    .WriteAsync
        })
    .AllowAnonymous();

/*
 * Readiness verifies dependencies required to serve
 * investigation traffic.
 */
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate =
                registration =>
                    registration.Tags
                        .Contains(
                            "ready"),

            ResponseWriter =
                HealthResponseWriter
                    .WriteAsync
        })
    .AllowAnonymous();

/*
 * Backward-compatible readiness endpoint.
 *
 * Existing local automation and Playwright continue
 * to use /health while callers migrate to
 * /health/ready.
 */
app.MapHealthChecks(
        "/health",
        new HealthCheckOptions
        {
            Predicate =
                registration =>
                    registration.Tags
                        .Contains(
                            "ready"),

            ResponseWriter =
                HealthResponseWriter
                    .WriteAsync
        })
    .AllowAnonymous();

/*
 * Apply checked-in EF Core migrations.
 *
 * Production deployments should run migrations as
 * a controlled deployment step rather than from
 * every application process.
 */
var runtimeProductionOptions =
    app.Services
        .GetRequiredService<
            IOptions<ProductionOptions>>()
        .Value;

if (runtimeProductionOptions
    .ApplyDatabaseMigrations)
{
    using var migrationScope =
        app.Services.CreateScope();

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
     * Prefer the authenticated subject identifier.
     * This remains stable even when the display name
     * changes.
     */
    var subject =
        httpContext.User.FindFirst(
            "sub")
        ?.Value;

    if (!string.IsNullOrWhiteSpace(
            subject))
    {
        return $"user:{subject}";
    }

    /*
     * Fall back to the authenticated name when the
     * token or local principal does not provide sub.
     */
    var userName =
        httpContext.User.Identity?.Name;

    if (!string.IsNullOrWhiteSpace(
            userName))
    {
        return $"user:{userName}";
    }

    /*
     * For local development, this normally resolves
     * to the loopback address.
     *
     * In production behind a trusted reverse proxy,
     * configure ForwardedHeaders before relying on
     * the remote IP address.
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
 * Keep this declaration at the absolute end of the
 * file. It enables WebApplicationFactory-based
 * integration tests.
 */
public partial class Program;