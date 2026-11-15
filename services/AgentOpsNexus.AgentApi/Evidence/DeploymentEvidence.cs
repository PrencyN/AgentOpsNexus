namespace AgentOpsNexus.AgentApi.Evidence;

public sealed record DeploymentEvidence(
    string EvidenceId,
    string ServiceName,
    string Environment,
    string Version,
    DateTimeOffset DeployedAt,
    string Status,
    string ChangeSummary,
    string CommitId,
    string PipelineRunId,
    string Source);