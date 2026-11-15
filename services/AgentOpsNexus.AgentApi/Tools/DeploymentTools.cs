using System.ComponentModel;
using System.Text.Json;
using AgentOpsNexus.AgentApi.Services.Deployments;

namespace AgentOpsNexus.AgentApi.Tools;

public sealed class DeploymentTools
{
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

    private readonly ILogger<DeploymentTools> _logger;

    public DeploymentTools(
        IDeploymentEvidenceService deploymentEvidenceService,
        ILogger<DeploymentTools> logger)
    {
        _deploymentEvidenceService =
            deploymentEvidenceService;

        _logger = logger;
    }

    [Description(
        "Gets recent deployment evidence for one named software service " +
        "and environment. Use this tool when an incident request refers " +
        "to a deployment, release, version, rollback, or recent change. " +
        "The tool is read-only and returns evidence IDs that must be " +
        "cited in the final answer.")]
    public async Task<string> GetRecentDeploymentsAsync(
        [Description(
            "Exact software service name, for example PaymentService " +
            "or CheckoutService.")]
        string serviceName,

        [Description(
            "Target environment. Allowed values are Development, Test, " +
            "Staging, or Production.")]
        string environment,

        [Description(
            "How many hours of deployment history to inspect. " +
            "Allowed range is 1 to 168 hours.")]
        int lookbackHours = 24,

        CancellationToken cancellationToken = default)
    {
        var normalizedServiceName =
            serviceName?.Trim() ?? string.Empty;

        var normalizedEnvironment =
            environment?.Trim() ?? string.Empty;

        if (normalizedServiceName.Length is < 2 or > 100)
        {
            return SerializeError(
                "INVALID_SERVICE_NAME",
                "Service name must contain between 2 and 100 characters.");
        }

        if (!AllowedEnvironments.Contains(
                normalizedEnvironment))
        {
            return SerializeError(
                "INVALID_ENVIRONMENT",
                "Environment must be Development, Test, Staging, " +
                "or Production.");
        }

        if (lookbackHours is < 1 or > 168)
        {
            return SerializeError(
                "INVALID_LOOKBACK",
                "Lookback hours must be between 1 and 168.");
        }

        _logger.LogInformation(
            "Agent tool GetRecentDeployments invoked for service " +
            "{ServiceName}, environment {Environment}, " +
            "lookback {LookbackHours} hours.",
            normalizedServiceName,
            normalizedEnvironment,
            lookbackHours);

        var evidence =
            await _deploymentEvidenceService
                .GetRecentDeploymentsAsync(
                    normalizedServiceName,
                    normalizedEnvironment,
                    lookbackHours,
                    cancellationToken);

        if (evidence.Count == 0)
        {
            _logger.LogInformation(
                "Agent tool GetRecentDeployments returned no evidence " +
                "for service {ServiceName} in {Environment}.",
                normalizedServiceName,
                normalizedEnvironment);

            return JsonSerializer.Serialize(
                new
                {
                    success = true,
                    tool = "GetRecentDeployments",
                    serviceName = normalizedServiceName,
                    environment = normalizedEnvironment,
                    lookbackHours,
                    evidenceCount = 0,
                    evidence = Array.Empty<object>(),
                    limitations =
                        "No matching deployment evidence was available. " +
                        "Do not infer that no deployment occurred."
                });
        }

        _logger.LogInformation(
            "Agent tool GetRecentDeployments returned {EvidenceCount} " +
            "evidence items: {EvidenceIds}.",
            evidence.Count,
            string.Join(
                ", ",
                evidence.Select(item => item.EvidenceId)));

        return JsonSerializer.Serialize(
            new
            {
                success = true,
                tool = "GetRecentDeployments",
                serviceName = normalizedServiceName,
                environment = normalizedEnvironment,
                lookbackHours,
                evidenceCount = evidence.Count,
                evidence = evidence.Select(item => new
                {
                    evidenceId = item.EvidenceId,
                    serviceName = item.ServiceName,
                    environment = item.Environment,
                    version = item.Version,
                    deployedAt = item.DeployedAt,
                    status = item.Status,
                    changeSummary = item.ChangeSummary,
                    commitId = item.CommitId,
                    pipelineRunId = item.PipelineRunId,
                    source = item.Source
                }),
                limitations =
                    "This milestone uses deterministic mock deployment " +
                    "records. Evidence does not come from a live deployment " +
                    "platform."
            });
    }

    private static string SerializeError(
        string code,
        string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                error = new
                {
                    code,
                    message
                }
            });
    }
}