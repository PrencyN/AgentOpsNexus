namespace AgentOpsNexus.AgentApi.Domain;

public enum AgentRequestStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}