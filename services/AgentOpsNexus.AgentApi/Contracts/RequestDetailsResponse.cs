namespace AgentOpsNexus.AgentApi.Contracts;

public sealed record RequestDetailsResponse(
    Guid RequestId,
    string Message,
    string Environment,
    string Severity,
    string Status,
    string? Response,
    string? ErrorCode,
    bool Retryable,
    int AttemptNumber,
    Guid? RetriedFromRequestId,
    string OwnerSubjectId,
    string OwnerDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);