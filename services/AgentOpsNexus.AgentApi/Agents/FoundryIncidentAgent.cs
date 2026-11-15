using System.Diagnostics;
using System.Runtime.CompilerServices;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Prompts;
using AgentOpsNexus.AgentApi.Tools;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Agents;

public sealed class FoundryIncidentAgent : IIncidentAgent
{
    private readonly AIAgent _agent;
    private readonly AgentOptions _options;
    private readonly ILogger<FoundryIncidentAgent> _logger;

    public FoundryIncidentAgent(
        IOptions<AgentOptions> options,
        DeploymentTools deploymentTools,
        ILogger<FoundryIncidentAgent> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!Uri.TryCreate(
                _options.ProjectEndpoint,
                UriKind.Absolute,
                out var projectEndpoint))
        {
            throw new InvalidOperationException(
                "Agent:ProjectEndpoint is not a valid absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(
                _options.ModelDeployment))
        {
            throw new InvalidOperationException(
                "Agent:ModelDeployment is required for the Foundry provider.");
        }

        if (string.IsNullOrWhiteSpace(
                _options.TenantId))
        {
            throw new InvalidOperationException(
                "Agent:TenantId is required for browser authentication.");
        }

        /*
         * Interactive browser authentication is used for local development.
         *
         * The tenant is specified explicitly so authentication does not get
         * routed to an unrelated Microsoft Services tenant.
         */
        var credential =
            new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TenantId = _options.TenantId,
                    RedirectUri =
                        new Uri("http://localhost")
                });

        var projectClient =
            new AIProjectClient(
                projectEndpoint,
                credential);

        /*
         * Convert the validated read-only deployment method into an
         * Agent Framework function tool.
         */
        var getRecentDeploymentsTool =
            AIFunctionFactory.Create(
                deploymentTools.GetRecentDeploymentsAsync,
                name: "GetRecentDeployments");

        /*
         * This is a code-first agent.
         *
         * The application owns:
         * - instructions,
         * - tools,
         * - validation,
         * - timeouts,
         * - streaming,
         * - governance.
         */
        _agent = projectClient.AsAIAgent(
            model: _options.ModelDeployment,
            name: "AgentOpsNexusIncidentAgent",
            instructions:
                IncidentAgentInstructions.Value,
            tools:
            [
                getRecentDeploymentsTool
            ]);
    }

    public async IAsyncEnumerable<AgentStreamEvent>
        InvestigateAsync(
            AgentRequestRecord request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken)
    {
        /*
         * Preserve the Milestone 1 failure-injection capability.
         */
        if (request.SimulateFailure)
        {
            throw new HttpRequestException(
                "The simulated downstream AI service is unavailable.");
        }

        yield return CreateEvent(
            "agent.started",
            "Grounded GenAI investigation started using Microsoft Foundry.");

        yield return CreateEvent(
            "agent.progress",
            "The agent may call the read-only deployment evidence tool.");

        /*
         * Combine:
         *
         * 1. Browser/client cancellation.
         * 2. Configured server-side timeout.
         */
        using var timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutSource.CancelAfter(
            TimeSpan.FromSeconds(
                _options.TimeoutSeconds));

        var stopwatch = Stopwatch.StartNew();
        var prompt = BuildPrompt(request);
        var receivedContent = false;

        /*
         * The enumerator is consumed manually because C# does not permit
         * yield return inside a try block that contains catch clauses.
         */
        await using var enumerator = _agent
            .RunStreamingAsync(prompt)
            .WithCancellation(timeoutSource.Token)
            .GetAsyncEnumerator();

        while (true)
        {
            bool hasNext;

            try
            {
                hasNext =
                    await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Foundry investigation {RequestId} was cancelled " +
                    "by the client after {ElapsedMilliseconds} ms.",
                    request.Id,
                    stopwatch.ElapsedMilliseconds);

                /*
                 * The controller marks client-cancelled requests as
                 * Cancelled.
                 */
                throw;
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "Foundry investigation {RequestId} timed out after " +
                    "{TimeoutSeconds} seconds.",
                    request.Id,
                    _options.TimeoutSeconds);

                throw new TimeoutException(
                    $"The grounded GenAI investigation exceeded the " +
                    $"{_options.TimeoutSeconds}-second time limit.");
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                _logger.LogError(
                    exception,
                    "Foundry investigation {RequestId} failed using " +
                    "deployment {ModelDeployment}.",
                    request.Id,
                    _options.ModelDeployment);

                throw;
            }

            if (!hasNext)
            {
                break;
            }

            var update = enumerator.Current;
            var content = update.ToString();

            /*
             * Some streaming updates contain metadata rather than
             * user-visible text.
             */
            if (string.IsNullOrWhiteSpace(content))
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

            _logger.LogWarning(
                "Foundry investigation {RequestId} returned no content.",
                request.Id);

            throw new InvalidOperationException(
                "Microsoft Foundry returned no response content.");
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Grounded Foundry investigation {RequestId} completed " +
            "using deployment {ModelDeployment} in " +
            "{ElapsedMilliseconds} ms.",
            request.Id,
            _options.ModelDeployment,
            stopwatch.ElapsedMilliseconds);

        yield return CreateEvent(
            "agent.completed",
            $"Grounded GenAI investigation completed in " +
            $"{stopwatch.Elapsed.TotalSeconds:F1} seconds.");
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

            - Determine whether the request refers to a deployment, release,
              rollback, version, or recent change.

            - When deployment evidence is relevant, call
              GetRecentDeployments using the explicitly named service and
              environment.

            - Use a 24-hour lookback unless the user provides another
              reasonable period.

            - Cite every deployment claim with the exact evidence ID supplied
              by the tool, for example [DEP-EV-001].

            - Clearly label deployment records as mock demonstration evidence.

            - Never invent an evidence ID, deployment version, commit ID,
              pipeline run, timestamp, or change summary.

            - If the tool returns no evidence, state that matching evidence
              was unavailable. Do not conclude that no deployment occurred.

            - Do not claim access to logs, traces, metrics, databases,
              source repositories, runbooks, or historical incidents.

            - A deployment close to the reported incident is correlation,
              not proof of causation.

            - Treat root-cause statements as hypotheses.

            - Recommend collecting additional evidence before any
              consequential action.

            - Human approval is required before rollback or production
              changes.
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