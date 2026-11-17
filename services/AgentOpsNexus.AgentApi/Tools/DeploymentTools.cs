using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Services.Deployments;

namespace AgentOpsNexus.AgentApi.Tools;

public sealed class DeploymentTools
{
    private const string ToolName =
        "GetRecentDeployments";

    private static readonly HashSet<string>
        AllowedEnvironments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Development",
            "Test",
            "Staging",
            "Production"
        };

    private readonly IDeploymentEvidenceService
        _deploymentEvidenceService;

    private readonly AgentOpsMetrics _metrics;

    private readonly ILogger<DeploymentTools>
        _logger;

    public DeploymentTools(
        IDeploymentEvidenceService
            deploymentEvidenceService,
        AgentOpsMetrics metrics,
        ILogger<DeploymentTools> logger)
    {
        _deploymentEvidenceService =
            deploymentEvidenceService;

        _metrics =
            metrics;

        _logger =
            logger;
    }

    [Description(
        "Gets recent deployment evidence for one named " +
        "software service and environment. Use this tool " +
        "when an incident request refers to a deployment, " +
        "release, version, rollback, or recent change. " +
        "The tool is read-only and returns evidence IDs " +
        "that must be cited in the final answer.")]
    public async Task<string>
        GetRecentDeploymentsAsync(
            [Description(
                "Exact software service name, for example " +
                "PaymentService or CheckoutService.")]
            string serviceName,

            [Description(
                "Target environment. Allowed values are " +
                "Development, Test, Staging, or Production.")]
            string environment,

            [Description(
                "How many hours of deployment history to " +
                "inspect. Allowed range is 1 to 168 hours.")]
            int lookbackHours = 24,

            CancellationToken cancellationToken = default)
    {
        var normalizedServiceName =
            serviceName?.Trim()
            ??
            string.Empty;

        var normalizedEnvironment =
            environment?.Trim()
            ??
            string.Empty;

        /*
         * Validation failures are returned as structured
         * tool results. No external operation occurs.
         */
        if (normalizedServiceName.Length
            is < 2 or > 100)
        {
            return SerializeError(
                "INVALID_SERVICE_NAME",
                "Service name must contain between " +
                "2 and 100 characters.");
        }

        if (!AllowedEnvironments.Contains(
                normalizedEnvironment))
        {
            return SerializeError(
                "INVALID_ENVIRONMENT",
                "Environment must be Development, Test, " +
                "Staging, or Production.");
        }

        if (lookbackHours is < 1 or > 168)
        {
            return SerializeError(
                "INVALID_LOOKBACK",
                "Lookback hours must be between " +
                "1 and 168.");
        }

        using var activity =
            AgentOpsActivitySource
                .StartToolInvocation(
                    toolName:
                        ToolName,
                    environment:
                        normalizedEnvironment);

        activity?.SetTag(
            "agentops.tool.lookback_hours",
            lookbackHours);

        /*
         * ServiceName is not recorded as a telemetry tag
         * because service names could become sensitive or
         * high-cardinality production data.
         */
        var stopwatch =
            Stopwatch.StartNew();

        _logger.LogInformation(
            "Agent tool {ToolName} invoked for service " +
            "{ServiceName}, environment {Environment}, " +
            "lookback {LookbackHours} hours.",
            ToolName,
            normalizedServiceName,
            normalizedEnvironment,
            lookbackHours);

        try
        {
            var evidence =
                await _deploymentEvidenceService
                    .GetRecentDeploymentsAsync(
                        normalizedServiceName,
                        normalizedEnvironment,
                        lookbackHours,
                        cancellationToken);

            stopwatch.Stop();

            activity?.SetTag(
                "agentops.tool.evidence_count",
                evidence.Count);

            activity?.SetTag(
                "agentops.tool.duration_ms",
                stopwatch.Elapsed.TotalMilliseconds);

            AgentOpsActivitySource.MarkSucceeded(
                activity);

            _metrics.RecordToolInvocation(
                toolName:
                    ToolName,
                status:
                    "completed",
                durationMilliseconds:
                    stopwatch.Elapsed.TotalMilliseconds,
                evidenceCount:
                    evidence.Count);

            if (evidence.Count == 0)
            {
                _logger.LogInformation(
                    "Agent tool {ToolName} returned no " +
                    "evidence for service {ServiceName} " +
                    "in {Environment}.",
                    ToolName,
                    normalizedServiceName,
                    normalizedEnvironment);

                return JsonSerializer.Serialize(
                    new
                    {
                        success = true,
                        tool = ToolName,
                        serviceName =
                            normalizedServiceName,
                        environment =
                            normalizedEnvironment,
                        lookbackHours,
                        evidenceCount = 0,
                        evidence =
                            Array.Empty<object>(),
                        limitations =
                            "No matching deployment evidence " +
                            "was available. Do not infer that " +
                            "no deployment occurred."
                    });
            }

            _logger.LogInformation(
                "Agent tool {ToolName} returned " +
                "{EvidenceCount} evidence items: " +
                "{EvidenceIds}.",
                ToolName,
                evidence.Count,
                string.Join(
                    ", ",
                    evidence.Select(
                        item =>
                            item.EvidenceId)));

            return JsonSerializer.Serialize(
                new
                {
                    success = true,
                    tool = ToolName,
                    serviceName =
                        normalizedServiceName,
                    environment =
                        normalizedEnvironment,
                    lookbackHours,
                    evidenceCount =
                        evidence.Count,
                    evidence =
                        evidence.Select(
                            item =>
                                new
                                {
                                    evidenceId =
                                        item.EvidenceId,
                                    serviceName =
                                        item.ServiceName,
                                    environment =
                                        item.Environment,
                                    version =
                                        item.Version,
                                    deployedAt =
                                        item.DeployedAt,
                                    status =
                                        item.Status,
                                    changeSummary =
                                        item.ChangeSummary,
                                    commitId =
                                        item.CommitId,
                                    pipelineRunId =
                                        item.PipelineRunId,
                                    source =
                                        item.Source
                                }),
                    limitations =
                        "This milestone uses deterministic " +
                        "mock deployment records. Evidence " +
                        "does not come from a live " +
                        "deployment platform."
                });
        }
        catch (OperationCanceledException exception)
        {
            stopwatch.Stop();

            activity?.SetTag(
                "agentops.cancellation.reason",
                "caller");

            AgentOpsActivitySource.MarkFailed(
                activity,
                exception);

            _metrics.RecordToolInvocation(
                toolName:
                    ToolName,
                status:
                    "cancelled",
                durationMilliseconds:
                    stopwatch.Elapsed.TotalMilliseconds,
                evidenceCount:
                    0);

            _logger.LogInformation(
                "Agent tool {ToolName} was cancelled " +
                "after {ElapsedMilliseconds} ms.",
                ToolName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            AgentOpsActivitySource.MarkFailed(
                activity,
                exception);

            _metrics.RecordToolInvocation(
                toolName:
                    ToolName,
                status:
                    "failed",
                durationMilliseconds:
                    stopwatch.Elapsed.TotalMilliseconds,
                evidenceCount:
                    0);

            _logger.LogError(
                exception,
                "Agent tool {ToolName} failed after " +
                "{ElapsedMilliseconds} ms.",
                ToolName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static string SerializeError(
        string code,
        string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                error =
                    new
                    {
                        code,
                        message
                    }
            });
    }
}