using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AgentOpsNexus.AgentApi.Health;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpContext httpContext,
        HealthReport report)
    {
        httpContext.Response.ContentType =
            "application/json";

        var response =
            new
            {
                status =
                    report.Status.ToString(),

                totalDurationMilliseconds =
                    Math.Round(
                        report.TotalDuration
                            .TotalMilliseconds,
                        2),

                checks =
                    report.Entries
                        .OrderBy(
                            entry =>
                                entry.Key)
                        .Select(
                            entry =>
                                new
                                {
                                    name =
                                        entry.Key,

                                    status =
                                        entry.Value
                                            .Status
                                            .ToString(),

                                    description =
                                        entry.Value
                                            .Description,

                                    durationMilliseconds =
                                        Math.Round(
                                            entry.Value
                                                .Duration
                                                .TotalMilliseconds,
                                            2)
                                })
                        .ToArray(),

                correlationId =
                    httpContext
                        .TraceIdentifier,

                timestamp =
                    DateTimeOffset.UtcNow
            };

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            response,
            JsonOptions,
            httpContext.RequestAborted);
    }
}