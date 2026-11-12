using System.Text;

namespace AgentOpsNexus.AgentApi.Domain;

public sealed class AgentRequestRecord
{
    private readonly StringBuilder _responseBuilder = new();

    public Guid Id { get; init; }

    public required string Message { get; init; }

    public required string Environment { get; init; }

    public required string Severity { get; init; }

    public bool SimulateFailure { get; init; }

    public AgentRequestStatus Status { get; private set; }

    public string? ErrorCode { get; private set; }

    public bool Retryable { get; private set; }

    public int AttemptNumber { get; init; } = 1;

    public Guid? RetriedFromRequestId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string Response => _responseBuilder.ToString();

    public void MarkProcessing()
    {
        Status = AgentRequestStatus.Processing;
        ErrorCode = null;
        Retryable = false;
    }

    public void AppendResponse(string content)
    {
        _responseBuilder.Append(content);
    }

    public void MarkCompleted()
    {
        Status = AgentRequestStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorCode = null;
        Retryable = false;
    }

    public void MarkFailed(string errorCode, bool retryable)
    {
        Status = AgentRequestStatus.Failed;
        ErrorCode = errorCode;
        Retryable = retryable;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = AgentRequestStatus.Cancelled;
        ErrorCode = "REQUEST_CANCELLED";
        Retryable = true;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}