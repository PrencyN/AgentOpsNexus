namespace AgentOpsNexus.AgentApi.Contracts;

public sealed record RequestAcceptedResponse(
    Guid RequestId,
    string Status,
    DateTimeOffset CreatedAt,
    string StreamUrl);