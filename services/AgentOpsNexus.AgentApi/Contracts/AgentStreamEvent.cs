namespace AgentOpsNexus.AgentApi.Contracts;

public sealed record AgentStreamEvent(
    string Type,
    string Message,
    DateTimeOffset Timestamp);