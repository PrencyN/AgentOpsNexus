using System.Text.Json;
using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AgentOpsNexus.AgentApi.Controllers;

[ApiController]
[Route("api/v1/requests")]
public sealed class AgentRequestsController
    : ControllerBase
{
    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly IAgentRequestRepository
        _repository;

    private readonly IIncidentAgent _agent;

    private readonly ILogger<AgentRequestsController>
        _logger;

    public AgentRequestsController(
        IAgentRequestRepository repository,
        IIncidentAgent agent,
        ILogger<AgentRequestsController> logger)
    {
        _repository = repository;
        _agent = agent;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<RequestAcceptedResponse>(
        StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<
        ActionResult<RequestAcceptedResponse>>
        Create(
            [FromBody] CreateAgentRequest request,
            CancellationToken cancellationToken)
    {
        var normalizedMessage =
            request.Message.Trim();

        if (normalizedMessage.Length < 10)
        {
            ModelState.AddModelError(
                nameof(request.Message),
                "Message must contain at least 10 " +
                "non-whitespace characters.");

            return ValidationProblem(ModelState);
        }

        var correlationId =
            HttpContext.Response.Headers[
                "X-Correlation-ID"]
                .FirstOrDefault()
            ??
            HttpContext.TraceIdentifier;

        var record =
            new AgentRequestRecord(
                id: Guid.NewGuid(),
                message: normalizedMessage,
                environment: request.Environment,
                severity: request.Severity,
                simulateFailure:
                    request.SimulateFailure,
                createdAt:
                    DateTimeOffset.UtcNow,
                correlationId:
                    correlationId);

        await _repository.AddAsync(
            record,
            cancellationToken);

        var streamUrl =
            Url.ActionLink(
                nameof(Stream),
                values:
                    new
                    {
                        requestId = record.Id
                    })
            ??
            $"/api/v1/requests/" +
            $"{record.Id}/stream";

        var response =
            new RequestAcceptedResponse(
                record.Id,
                record.Status.ToString(),
                record.CreatedAt,
                streamUrl);

        return Accepted(
            streamUrl,
            response);
    }

    [HttpGet]
    [ProducesResponseType<
        IReadOnlyCollection<RequestDetailsResponse>>(
            StatusCodes.Status200OK)]
    public async Task<
        ActionResult<
            IReadOnlyCollection<
                RequestDetailsResponse>>>
        GetAll(
            CancellationToken cancellationToken)
    {
        var requests =
            await _repository.GetAllAsync(
                cancellationToken);

        var response =
            requests
                .Select(MapResponse)
                .ToArray();

        return Ok(response);
    }

    [HttpGet("{requestId:guid}")]
    [ProducesResponseType<RequestDetailsResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<RequestDetailsResponse>>
        GetById(
            Guid requestId,
            CancellationToken cancellationToken)
    {
        var request =
            await _repository.GetAsync(
                requestId,
                cancellationToken);

        if (request is null)
        {
            return NotFound(
                CreateProblem(
                    StatusCodes.Status404NotFound,
                    "REQUEST_NOT_FOUND",
                    "Request not found",
                    $"No request exists with ID " +
                    $"{requestId}."));
        }

        return Ok(MapResponse(request));
    }

    [HttpPost("{requestId:guid}/retry")]
    [ProducesResponseType<RequestAcceptedResponse>(
        StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<RequestAcceptedResponse>>
        Retry(
            Guid requestId,
            CancellationToken cancellationToken)
    {
        var original =
            await _repository.GetAsync(
                requestId,
                cancellationToken);

        if (original is null)
        {
            return NotFound(
                CreateProblem(
                    StatusCodes.Status404NotFound,
                    "REQUEST_NOT_FOUND",
                    "Request not found",
                    $"No request exists with ID " +
                    $"{requestId}."));
        }

        if (!original.Retryable)
        {
            return BadRequest(
                CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "REQUEST_NOT_RETRYABLE",
                    "Request cannot be retried",
                    "Only failed or cancelled " +
                    "retryable requests can be retried."));
        }

        var correlationId =
            HttpContext.Response.Headers[
                "X-Correlation-ID"]
                .FirstOrDefault()
            ??
            HttpContext.TraceIdentifier;

        var retry =
            new AgentRequestRecord(
                id: Guid.NewGuid(),
                message: original.Message,
                environment:
                    original.Environment,
                severity:
                    original.Severity,
                simulateFailure: false,
                attemptNumber:
                    original.AttemptNumber + 1,
                retriedFromRequestId:
                    original.Id,
                createdAt:
                    DateTimeOffset.UtcNow,
                correlationId:
                    correlationId);

        await _repository.AddAsync(
            retry,
            cancellationToken);

        var streamUrl =
            Url.ActionLink(
                nameof(Stream),
                values:
                    new
                    {
                        requestId = retry.Id
                    })
            ??
            $"/api/v1/requests/" +
            $"{retry.Id}/stream";

        return Accepted(
            streamUrl,
            new RequestAcceptedResponse(
                retry.Id,
                retry.Status.ToString(),
                retry.CreatedAt,
                streamUrl));
    }

    [HttpGet("{requestId:guid}/stream")]
    public async Task Stream(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var request =
            await _repository.GetAsync(
                requestId,
                cancellationToken);

        if (request is null)
        {
            Response.StatusCode =
                StatusCodes.Status404NotFound;

            await Response.WriteAsJsonAsync(
                CreateProblem(
                    StatusCodes.Status404NotFound,
                    "REQUEST_NOT_FOUND",
                    "Request not found",
                    $"No request exists with ID " +
                    $"{requestId}."),
                cancellationToken);

            return;
        }

        if (request.Status ==
            AgentRequestStatus.Completed)
        {
            Response.StatusCode =
                StatusCodes.Status409Conflict;

            await Response.WriteAsJsonAsync(
                CreateProblem(
                    StatusCodes.Status409Conflict,
                    "REQUEST_ALREADY_COMPLETED",
                    "Request already completed",
                    "A completed request cannot " +
                    "be streamed again."),
                cancellationToken);

            return;
        }

        Response.StatusCode =
            StatusCodes.Status200OK;

        Response.ContentType =
            "text/event-stream";

        Response.Headers.CacheControl =
            "no-cache";

        Response.Headers.Connection =
            "keep-alive";

        Response.Headers["X-Accel-Buffering"] =
            "no";

        request.MarkProcessing();

        await _repository.SaveChangesAsync(
            cancellationToken);

        try
        {
            await foreach (
                var update in
                    _agent.InvestigateAsync(
                        request,
                        cancellationToken))
            {
                if (update.Type ==
                    "agent.message.delta")
                {
                    request.AppendResponse(
                        update.Message);
                }

                await WriteEventAsync(
                    update,
                    cancellationToken);
            }

            request.MarkCompleted();

            await _repository.SaveChangesAsync(
                CancellationToken.None);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested)
        {
            request.MarkCancelled();

            await SaveWithoutCancellationAsync();

            _logger.LogInformation(
                "Agent request {RequestId} " +
                "was cancelled. Correlation ID: " +
                "{CorrelationId}.",
                request.Id,
                request.CorrelationId);
        }
        catch (Exception exception)
        {
            request.MarkFailed(
                "AGENT_SERVICE_UNAVAILABLE",
                retryable: true);

            await SaveWithoutCancellationAsync();

            _logger.LogError(
                exception,
                "Agent request {RequestId} failed. " +
                "Correlation ID: {CorrelationId}.",
                request.Id,
                request.CorrelationId);

            var failureEvent =
                new AgentStreamEvent(
                    "agent.failed",
                    "The AI service is temporarily " +
                    "unavailable. You can retry this " +
                    "request.",
                    DateTimeOffset.UtcNow);

            await WriteEventAsync(
                failureEvent,
                CancellationToken.None);
        }
    }

    private async Task SaveWithoutCancellationAsync()
    {
        try
        {
            await _repository.SaveChangesAsync(
                CancellationToken.None);
        }
        catch (Exception persistenceException)
        {
            _logger.LogError(
                persistenceException,
                "Failed to persist final request state.");
        }
    }

    private async Task WriteEventAsync(
        AgentStreamEvent update,
        CancellationToken cancellationToken)
    {
        var json =
            JsonSerializer.Serialize(
                update,
                JsonOptions);

        await Response.WriteAsync(
            $"event: {update.Type}\n",
            cancellationToken);

        await Response.WriteAsync(
            $"data: {json}\n\n",
            cancellationToken);

        await Response.Body.FlushAsync(
            cancellationToken);
    }

    private static RequestDetailsResponse
        MapResponse(
            AgentRequestRecord request)
    {
        return new RequestDetailsResponse(
            request.Id,
            request.Message,
            request.Environment,
            request.Severity,
            request.Status.ToString(),
            string.IsNullOrWhiteSpace(
                request.Response)
                ? null
                : request.Response,
            request.ErrorCode,
            request.Retryable,
            request.AttemptNumber,
            request.RetriedFromRequestId,
            request.CreatedAt,
            request.CompletedAt);
    }

    private static ProblemDetails CreateProblem(
        int status,
        string code,
        string title,
        string detail)
    {
        var problem =
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type =
                    "https://agentops-nexus/errors/" +
                    code.ToLowerInvariant()
            };

        problem.Extensions["code"] =
            code;

        problem.Extensions["retryable"] =
            status >= 500;

        return problem;
    }
}