using AgentOpsNexus.AgentApi.Evidence;

namespace AgentOpsNexus.AgentApi.Services.Deployments;

public sealed class MockDeploymentEvidenceService
    : IDeploymentEvidenceService
{
    private static readonly IReadOnlyCollection<DeploymentEvidence>
        DeploymentEvidenceItems =
        new[]
        {
            new DeploymentEvidence(
                EvidenceId: "DEP-EV-001",
                ServiceName: "PaymentService",
                Environment: "Development",
                Version: "2.4.1",
                DeployedAt: new DateTimeOffset(
                    2026,
                    11,
                    15,
                    8,
                    30,
                    0,
                    TimeSpan.Zero),
                Status: "Succeeded",
                ChangeSummary:
                    "Updated payment-provider timeout handling and " +
                    "connection-pool configuration.",
                CommitId: "a91f2d7",
                PipelineRunId: "PIPE-1842",
                Source: "Mock deployment evidence"),

            new DeploymentEvidence(
                EvidenceId: "DEP-EV-002",
                ServiceName: "PaymentService",
                Environment: "Development",
                Version: "2.4.0",
                DeployedAt: new DateTimeOffset(
                    2026,
                    11,
                    14,
                    14,
                    15,
                    0,
                    TimeSpan.Zero),
                Status: "Succeeded",
                ChangeSummary:
                    "Added structured logging around payment-provider " +
                    "responses.",
                CommitId: "7bd83ac",
                PipelineRunId: "PIPE-1827",
                Source: "Mock deployment evidence"),

            new DeploymentEvidence(
                EvidenceId: "DEP-EV-003",
                ServiceName: "CheckoutService",
                Environment: "Development",
                Version: "3.8.2",
                DeployedAt: new DateTimeOffset(
                    2026,
                    11,
                    15,
                    7,
                    45,
                    0,
                    TimeSpan.Zero),
                Status: "Succeeded",
                ChangeSummary:
                    "Updated checkout orchestration and retry policy.",
                CommitId: "2de9b81",
                PipelineRunId: "PIPE-1839",
                Source: "Mock deployment evidence"),

            new DeploymentEvidence(
                EvidenceId: "DEP-EV-004",
                ServiceName: "PaymentService",
                Environment: "Production",
                Version: "2.3.9",
                DeployedAt: new DateTimeOffset(
                    2026,
                    11,
                    13,
                    10,
                    0,
                    0,
                    TimeSpan.Zero),
                Status: "Succeeded",
                ChangeSummary:
                    "Security patch and dependency updates.",
                CommitId: "b08ca14",
                PipelineRunId: "PIPE-1795",
                Source: "Mock deployment evidence")
        };

    public Task<IReadOnlyCollection<DeploymentEvidence>>
        GetRecentDeploymentsAsync(
            string serviceName,
            string environment,
            int lookbackHours,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedServiceName =
            serviceName.Trim();

        var normalizedEnvironment =
            environment.Trim();

        /*
         * In this milestone, lookbackHours is validated by the tool.
         * Mock records are filtered by service and environment only so the
         * demonstration remains deterministic and repeatable.
         */
        var matches = DeploymentEvidenceItems
            .Where(item =>
                item.ServiceName.Equals(
                    normalizedServiceName,
                    StringComparison.OrdinalIgnoreCase)
                &&
                item.Environment.Equals(
                    normalizedEnvironment,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.DeployedAt)
            .ToArray();

        return Task.FromResult<
            IReadOnlyCollection<DeploymentEvidence>>(matches);
    }
}