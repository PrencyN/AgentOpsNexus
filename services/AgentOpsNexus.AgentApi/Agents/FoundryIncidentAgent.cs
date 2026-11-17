using System.Diagnostics;
using System.Runtime.CompilerServices;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Prompts;
using AgentOpsNexus.AgentApi.Tools;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Agents;

public sealed class FoundryIncidentAgent
    : IIncidentAgent
{
    private const string ProviderName =
        "MicrosoftFoundry";

    private readonly AIAgent _agent;

    private readonly AgentOptions _options;

    private readonly AgentOpsMetrics _metrics;

    private readonly ILogger<FoundryIncidentAgent>
        _logger;

    public FoundryIncidentAgent(
        IOptions<AgentOptions> options,
        DeploymentTools deploymentTools,
        AgentOpsMetrics metrics,
        ILogger<FoundryIncidentAgent> logger)
    {
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;

        if (!Uri.TryCreate(
                _options.ProjectEndpoint,
                UriKind.Absolute,
                out var projectEndpoint))
        {
            throw new InvalidOperationException(
                "Agent:ProjectEndpoint is not a valid " +
                "absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(
                _options.ModelDeployment))
        {
            throw new InvalidOperationException(
                "Agent:ModelDeployment is required " +
                "for the Foundry provider.");
        }

        if (string.IsNullOrWhiteSpace(
                _options.TenantId))
        {
            throw new InvalidOperationException(
                "Agent:TenantId is required for " +
                "browser authentication.");
        }

        /*
         * Interactive browser authentication is used
         * for local development.
         *
         * TenantId is explicitly configured so the
         * authentication flow targets the Azure tenant
         * that contains the Foundry project.
         */
        var credential =
            new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TenantId =
                        _options.TenantId,

                    RedirectUri =
                        new Uri(
                            "http://localhost")
                });

        var projectClient =
            new AIProjectClient(
                projectEndpoint,
                credential);

        /*
         * Convert the read-only deployment lookup method
         * into an Agent Framework function tool.
         */
        var getRecentDeploymentsTool =
            AIFunctionFactory.Create(
                deploymentTools
                    .GetRecentDeploymentsAsync,
                name:
                    "GetRecentDeployments");

        _agent = projectClient.AsAIAgent(
            model:
                _options.ModelDeployment,
            name:
                "AgentOpsNexusIncidentAgent",
            instructions:
                IncidentAgentInstructions.Value,
            tools:
            [
                getRecentDeploymentsTool
            ]);
    }

    public async IAsyncEnumerable<
        AgentStreamEvent>
        InvestigateAsync(
            AgentRequestRecord request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
    {
        /*
         * Start a custom activity without recording the
         * incident prompt or model response.
         */
        using var activity =
            AgentOpsActivitySource
                .StartAgentInvestigation(
                    requestId:
                        request.Id,
                    provider:
                        ProviderName,
                    environment:
                        request.Environment,
                    severity:
                        request.Severity,
                    attemptNumber:
                        request.AttemptNumber);

        activity?.SetTag(
            "gen_ai.request.model",
            _options.ModelDeployment);

        activity?.SetTag(
            "agentops.simulated_failure",
            request.SimulateFailure);

        var stopwatch =
            Stopwatch.StartNew();

        /*
         * Preserve the Milestone 1 failure-injection
         * capability while recording its telemetry.
         */
        if (request.SimulateFailure)
        {
            stopwatch.Stop();

            var exception =
                new HttpRequestException(
                    "The simulated downstream AI " +
                    "service is unavailable.");

            AgentOpsActivitySource.MarkFailed(
                activity,
                exception);

            _metrics.RecordInvestigation(
                provider:
                    ProviderName,
                status:
                    "failed",
                durationMilliseconds:
                    stopwatch.Elapsed.TotalMilliseconds);

            throw exception;
        }

        yield return CreateEvent(
            "agent.started",
            "Grounded GenAI investigation started " +
            "using Microsoft Foundry.");

        yield return CreateEvent(
            "agent.progress",
            "The agent may call the read-only " +
            "deployment evidence tool.");

        /*
         * Combine browser cancellation and the configured
         * server-side timeout.
         */
        using var timeoutSource =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        timeoutSource.CancelAfter(
            TimeSpan.FromSeconds(
                _options.TimeoutSeconds));

        var prompt =
            BuildPrompt(request);

        var receivedContent =
            false;

        /*
         * The asynchronous enumerator is consumed manually.
         *
         * C# does not allow yield return inside a try block
         * that contains catch clauses.
         */
        await using var enumerator =
            _agent
                .RunStreamingAsync(prompt)
                .WithCancellation(
                    timeoutSource.Token)
                .GetAsyncEnumerator();

        while (true)
        {
            bool hasNext;

            try
            {
                hasNext =
                    await enumerator
                        .MoveNextAsync();
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                stopwatch.Stop();

                activity?.SetTag(
                    "agentops.cancellation.reason",
                    "client");

                activity?.SetStatus(
                    ActivityStatusCode.Error,
                    "Client cancelled the investigation.");

                _metrics.RecordInvestigation(
                    provider:
                        ProviderName,
                    status:
                        "cancelled",
                    durationMilliseconds:
                        stopwatch.Elapsed
                            .TotalMilliseconds);

                _logger.LogInformation(
                    "Foundry investigation " +
                    "{RequestId} was cancelled by the " +
                    "client after " +
                    "{ElapsedMilliseconds} ms.",
                    request.Id,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (OperationCanceledException exception)
                when (
                    timeoutSource
                        .IsCancellationRequested)
            {
                stopwatch.Stop();

                activity?.SetTag(
                    "agentops.cancellation.reason",
                    "timeout");

                AgentOpsActivitySource.MarkFailed(
                    activity,
                    exception);

                _metrics.RecordInvestigation(
                    provider:
                        ProviderName,
                    status:
                        "timeout",
                    durationMilliseconds:
                        stopwatch.Elapsed
                            .TotalMilliseconds);

                _logger.LogWarning(
                    "Foundry investigation " +
                    "{RequestId} timed out after " +
                    "{TimeoutSeconds} seconds.",
                    request.Id,
                    _options.TimeoutSeconds);

                throw new TimeoutException(
                    "The grounded GenAI investigation " +
                    "exceeded the configured " +
                    $"{_options.TimeoutSeconds}-second " +
                    "time limit.",
                    exception);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                AgentOpsActivitySource.MarkFailed(
                    activity,
                    exception);

                _metrics.RecordInvestigation(
                    provider:
                        ProviderName,
                    status:
                        "failed",
                    durationMilliseconds:
                        stopwatch.Elapsed
                            .TotalMilliseconds);

                _logger.LogError(
                    exception,
                    "Foundry investigation " +
                    "{RequestId} failed using deployment " +
                    "{ModelDeployment}.",
                    request.Id,
                    _options.ModelDeployment);

                throw;
            }

            if (!hasNext)
            {
                break;
            }

            var update =
                enumerator.Current;

            var content =
                update.ToString();

            /*
             * Some provider updates contain metadata only.
             */
            if (string.IsNullOrWhiteSpace(
                    content))
            {
                continue;
            }

            receivedContent = true;

            yield return CreateEvent(
                "agent.message.delta",
                content);
        }

        if (!receivedContent)
        {
            stopwatch.Stop();

            var exception =
                new InvalidOperationException(
                    "Microsoft Foundry returned no " +
                    "response content.");

            AgentOpsActivitySource.MarkFailed(
                activity,
                exception);

            _metrics.RecordInvestigation(
                provider:
                    ProviderName,
                status:
                    "empty",
                durationMilliseconds:
                    stopwatch.Elapsed.TotalMilliseconds);

            _logger.LogWarning(
                "Foundry investigation {RequestId} " +
                "returned no content.",
                request.Id);

            throw exception;
        }

        stopwatch.Stop();

        activity?.SetTag(
            "agentops.response.received",
            true);

        activity?.SetTag(
            "agentops.duration_ms",
            stopwatch.Elapsed.TotalMilliseconds);

        AgentOpsActivitySource.MarkSucceeded(
            activity);

        _metrics.RecordInvestigation(
            provider:
                ProviderName,
            status:
                "completed",
            durationMilliseconds:
                stopwatch.Elapsed.TotalMilliseconds);

        _logger.LogInformation(
            "Grounded Foundry investigation " +
            "{RequestId} completed using deployment " +
            "{ModelDeployment} in " +
            "{ElapsedMilliseconds} ms.",
            request.Id,
            _options.ModelDeployment,
            stopwatch.ElapsedMilliseconds);

        yield return CreateEvent(
            "agent.completed",
            "Grounded GenAI investigation completed " +
            $"in {stopwatch.Elapsed.TotalSeconds:F1} " +
            "seconds.");
    }

    private static string BuildPrompt(
        AgentRequestRecord request)
    {
        return $$"""
            Analyze the following operational incident request.

            Incident description:
            {{request.Message}}

            Environment:
            {{request.Environment}}

            Reported severity:
            {{request.Severity}}

            Investigation requirements:

            - Determine whether the request refers to a deployment,
              release, rollback, version, or recent change.

            - When deployment evidence is relevant, call
              GetRecentDeployments using the explicitly named service
              and environment.

            - Use a 24-hour lookback unless the user provides another
              reasonable period.

            - Cite every deployment claim with the exact evidence ID
              supplied by the tool, for example [DEP-EV-001].

            - Clearly label deployment records as mock demonstration
              evidence.

            - Never invent an evidence ID, deployment version,
              commit ID, pipeline run, timestamp, or change summary.

            - If the tool returns no evidence, state that matching
              evidence was unavailable. Do not conclude that no
              deployment occurred.

            - Do not claim access to logs, traces, metrics,
              databases, source repositories, runbooks, or
              historical incidents.

            - A deployment close to the reported incident is
              correlation, not proof of causation.

            - Treat root-cause statements as hypotheses.

            - Recommend collecting additional evidence before any
              consequential action.

            - Human approval is required before rollback or
              production changes.

            - Put a blank line after every Markdown heading.
            """;
    }

    private static AgentStreamEvent CreateEvent(
        string type,
        string message)
    {
        return new AgentStreamEvent(
            type,
            message,
            DateTimeOffset.UtcNow);
    }
}