using AgentOpsNexus.AgentApi.Evidence;

namespace AgentOpsNexus.AgentApi.Services.Deployments;

public interface IDeploymentEvidenceService
{
    Task<IReadOnlyCollection<DeploymentEvidence>>
        GetRecentDeploymentsAsync(
            string serviceName,
            string environment,
            int lookbackHours,
            CancellationToken cancellationToken);
}