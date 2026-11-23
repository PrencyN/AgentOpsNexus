using System.Text.Json;
using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using AgentOpsNexus.AgentApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    private readonly IIncidentAgent
        _agent;

    private readonly IncidentInputPolicy
        _inputPolicy;

    private readonly ILogger<
        AgentRequestsController>
        _logger;

    public AgentRequestsController(
        IAgentRequestRepository repository,
        IIncidentAgent agent,
        IncidentInputPolicy inputPolicy,
        ILogger<AgentRequestsController> logger)
    {
        _repository =
            repository;

        _agent =
            agent;

        _inputPolicy =
            inputPolicy;

        _logger =
            logger;
    }

    [HttpPost]
    [Authorize(
        Policy =
            AgentOpsPolicies.CreateRequests)]
    [EnableRateLimiting(
        "investigation-create")]
    [ProducesResponseType<
        RequestAcceptedResponse>(
            StatusCodes.Status202Accepted)]
    [ProducesResponseType<
        ValidationProblemDetails>(
            StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status429TooManyRequests)]
    public async Task<
        ActionResult<RequestAcceptedResponse>>
        Create(
            [FromBody]
            CreateAgentRequest request,
            CancellationToken cancellationToken)
    {
        var policyResult =
            _inputPolicy.Evaluate(
                request.Message);

        if (!policyResult.IsAccepted)
        {
            var problem =
                CreateProblem(
                    status:
                        StatusCodes
                            .Status400BadRequest,

                    code:
                        policyResult.ErrorCode
                        ??
                        "INVALID_INCIDENT_DESCRIPTION",

                    title:
                        "Incident description rejected",

                    detail:
                        policyResult.ErrorMessage
                        ??
                        "The incident description " +
                        "is invalid.",

                    retryable:
                        false);

            return BadRequest(
                problem);
        }

        var correlationId =
            HttpContext.TraceIdentifier;

        var record =
            new AgentRequestRecord(
                id:
                    Guid.NewGuid(),

                message:
                    policyResult.NormalizedInput!,

                environment:
                    request.Environment,

                severity:
                    request.Severity,

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
                        requestId =
                            record.Id
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
    [Authorize(
        Policy =
            AgentOpsPolicies.ReadRequests)]
    [ProducesResponseType<
        IReadOnlyCollection<
            RequestDetailsResponse>>(
                StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
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
                .Select(
                    MapResponse)
                .ToArray();

        return Ok(
            response);
    }

    [HttpGet("{requestId:guid}")]
    [Authorize(
        Policy =
            AgentOpsPolicies.ReadRequests)]
    [ProducesResponseType<
        RequestDetailsResponse>(
            StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
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
                    status:
                        StatusCodes
                            .Status404NotFound,

                    code:
                        "REQUEST_NOT_FOUND",

                    title:
                        "Request not found",

                    detail:
                        $"No request exists with ID " +
                        $"{requestId}.",

                    retryable:
                        false));
        }

        return Ok(
            MapResponse(
                request));
    }

    [HttpPost("{requestId:guid}/retry")]
    [Authorize(
        Policy =
            AgentOpsPolicies.CreateRequests)]
    [EnableRateLimiting(
        "investigation-create")]
    [ProducesResponseType<
        RequestAcceptedResponse>(
            StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status429TooManyRequests)]
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
                    status:
                        StatusCodes
                            .Status404NotFound,

                    code:
                        "REQUEST_NOT_FOUND",

                    title:
                        "Request not found",

                    detail:
                        $"No request exists with ID " +
                        $"{requestId}.",

                    retryable:
                        false));
        }

        if (!original.Retryable)
        {
            return BadRequest(
                CreateProblem(
                    status:
                        StatusCodes
                            .Status400BadRequest,

                    code:
                        "REQUEST_NOT_RETRYABLE",

                    title:
                        "Request cannot be retried",

                    detail:
                        "Only failed or cancelled " +
                        "retryable requests can be " +
                        "retried.",

                    retryable:
                        false));
        }

        /*
         * The persisted message already passed the
         * IncidentInputPolicy during the original
         * request. Re-evaluate it as defense in depth.
         */
        var policyResult =
            _inputPolicy.Evaluate(
                original.Message);

        if (!policyResult.IsAccepted)
        {
            return BadRequest(
                CreateProblem(
                    status:
                        StatusCodes
                            .Status400BadRequest,

                    code:
                        policyResult.ErrorCode
                        ??
                        "INVALID_INCIDENT_DESCRIPTION",

                    title:
                        "Stored incident description " +
                        "rejected",

                    detail:
                        policyResult.ErrorMessage
                        ??
                        "The stored incident " +
                        "description is invalid.",

                    retryable:
                        false));
        }

        var retry =
            new AgentRequestRecord(
                id:
                    Guid.NewGuid(),

                message:
                    policyResult.NormalizedInput!,

                environment:
                    original.Environment,

                severity:
                    original.Severity,

                simulateFailure:
                    false,

                attemptNumber:
                    original.AttemptNumber + 1,

                retriedFromRequestId:
                    original.Id,

                createdAt:
                    DateTimeOffset.UtcNow,

                correlationId:
                    HttpContext.TraceIdentifier);

        await _repository.AddAsync(
            retry,
            cancellationToken);

        var streamUrl =
            Url.ActionLink(
                nameof(Stream),
                values:
                    new
                    {
                        requestId =
                            retry.Id
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
    [Authorize(
        Policy =
            AgentOpsPolicies.ReadRequests)]
    [EnableRateLimiting(
        "investigation-stream")]
    [ProducesResponseType(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status429TooManyRequests)]
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
                StatusCodes
                    .Status404NotFound;

            await Response.WriteAsJsonAsync(
                CreateProblem(
                    status:
                        StatusCodes
                            .Status404NotFound,

                    code:
                        "REQUEST_NOT_FOUND",

                    title:
                        "Request not found",

                    detail:
                        $"No request exists with ID " +
                        $"{requestId}.",

                    retryable:
                        false),

                cancellationToken);

            return;
        }

        if (request.Status ==
            AgentRequestStatus.Completed)
        {
            Response.StatusCode =
                StatusCodes
                    .Status409Conflict;

            await Response.WriteAsJsonAsync(
                CreateProblem(
                    status:
                        StatusCodes
                            .Status409Conflict,

                    code:
                        "REQUEST_ALREADY_COMPLETED",

                    title:
                        "Request already completed",

                    detail:
                        "A completed request cannot " +
                        "be streamed again.",

                    retryable:
                        false),

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

        Response.Headers[
            "X-Accel-Buffering"] =
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
                "Agent request {RequestId} was " +
                "cancelled. Correlation ID: " +
                "{CorrelationId}.",
                request.Id,
                request.CorrelationId);
        }
        catch (Exception exception)
        {
            request.MarkFailed(
                "AGENT_SERVICE_UNAVAILABLE",
                retryable:
                    true);

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

    private async Task
        SaveWithoutCancellationAsync()
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
                "Failed to persist the final " +
                "request state.");
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

    private ProblemDetails CreateProblem(
        int status,
        string code,
        string title,
        string detail,
        bool retryable)
    {
        var problem =
            new ProblemDetails
            {
                Status =
                    status,

                Title =
                    title,

                Detail =
                    detail,

                Type =
                    string.Concat(
                        "https",
                        "://",
                        "agentops-nexus",
                        "/errors/",
                        code.ToLowerInvariant())
            };

        problem.Extensions["code"] =
            code;

        problem.Extensions["retryable"] =
            retryable;

        problem.Extensions["correlationId"] =
            HttpContext.TraceIdentifier;

        return problem;
    }
}