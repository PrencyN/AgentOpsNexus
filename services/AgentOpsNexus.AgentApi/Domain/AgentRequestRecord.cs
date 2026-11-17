namespace AgentOpsNexus.AgentApi.Domain;

public sealed class AgentRequestRecord
{
    private AgentRequestRecord()
    {
        /*
         * Required by Entity Framework Core.
         */
    }

    public AgentRequestRecord(
        Guid id,
        string message,
        string environment,
        string severity,
        bool simulateFailure,
        DateTimeOffset createdAt,
        int attemptNumber = 1,
        Guid? retriedFromRequestId = null,
        string? correlationId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Request ID cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Message is required.",
                nameof(message));
        }

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException(
                "Environment is required.",
                nameof(environment));
        }

        if (string.IsNullOrWhiteSpace(severity))
        {
            throw new ArgumentException(
                "Severity is required.",
                nameof(severity));
        }

        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptNumber),
                "Attempt number must be at least one.");
        }

        Id = id;
        Message = message.Trim();
        Environment = environment.Trim();
        Severity = severity.Trim();
        SimulateFailure = simulateFailure;
        Status = AgentRequestStatus.Queued;
        AttemptNumber = attemptNumber;
        RetriedFromRequestId = retriedFromRequestId;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
        Response = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Message { get; private set; } =
        string.Empty;

    public string Environment { get; private set; } =
        string.Empty;

    public string Severity { get; private set; } =
        string.Empty;

    public bool SimulateFailure { get; private set; }

    public AgentRequestStatus Status { get; private set; }

    public string Response { get; private set; } =
        string.Empty;

    public string? ErrorCode { get; private set; }

    public bool Retryable { get; private set; }

    public int AttemptNumber { get; private set; }

    public Guid? RetriedFromRequestId { get; private set; }

    public AgentRequestRecord? RetriedFromRequest
    {
        get;
        private set;
    }

    public ICollection<AgentRequestRecord> RetryAttempts
    {
        get;
        private set;
    } = new List<AgentRequestRecord>();

    public string? CorrelationId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkProcessing()
    {
        if (Status == AgentRequestStatus.Completed)
        {
            throw new InvalidOperationException(
                "A completed request cannot be processed again.");
        }

        Status = AgentRequestStatus.Processing;
        StartedAt ??= DateTimeOffset.UtcNow;
        ErrorCode = null;
        Retryable = false;
        CompletedAt = null;
    }

    public void AppendResponse(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        Response += content;
    }

    public void MarkCompleted()
    {
        Status = AgentRequestStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorCode = null;
        Retryable = false;
    }

    public void MarkFailed(
        string errorCode,
        bool retryable)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException(
                "Error code is required.",
                nameof(errorCode));
        }

        Status = AgentRequestStatus.Failed;
        ErrorCode = errorCode.Trim();
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

    public void SetCorrelationId(
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        CorrelationId = correlationId.Trim();
    }
}