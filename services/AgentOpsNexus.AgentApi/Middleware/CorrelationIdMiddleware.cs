using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentOpsNexus.AgentApi.Middleware;

public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName =
        "X-Correlation-ID";

    public const string ItemName =
        "AgentOps.CorrelationId";

    private const int MaximumLength = 100;

    private readonly RequestDelegate _next;

    private readonly ILogger<
        CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        var suppliedCorrelationId =
            context.Request.Headers[HeaderName]
                .FirstOrDefault();

        var correlationId =
            IsValid(suppliedCorrelationId)
                ? suppliedCorrelationId!
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;

        context.Items[ItemName] = correlationId;

        context.Response.Headers[HeaderName] =
            correlationId;

        Activity.Current?.SetTag(
            "agentops.correlation_id",
            correlationId);

        using var loggingScope =
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] =
                        correlationId,

                    ["TraceId"] =
                        Activity.Current?
                            .TraceId
                            .ToString()
                        ??
                        string.Empty
                });

        await _next(context);
    }

    private static bool IsValid(
        string? correlationId)
    {
        return
            !string.IsNullOrWhiteSpace(
                correlationId)
            &&
            correlationId.Length <= MaximumLength
            &&
            ValidCorrelationIdPattern()
                .IsMatch(correlationId);
    }

    [GeneratedRegex(
        "^[A-Za-z0-9][A-Za-z0-9._:-]*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex
        ValidCorrelationIdPattern();
}