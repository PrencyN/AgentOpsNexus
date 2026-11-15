using System.Diagnostics;
using System.Runtime.CompilerServices;
using AgentOpsNexus.AgentApi.Configuration;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Prompts;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Agents;

public sealed class FoundryIncidentAgent : IIncidentAgent
{
    private readonly AIAgent _agent;
    private readonly AgentOptions _options;
    private readonly ILogger<FoundryIncidentAgent> _logger;

    public FoundryIncidentAgent(
        IOptions<AgentOptions> options,
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

        if (string.IsNullOrWhiteSpace(_options.ModelDeployment))
        {
            throw new InvalidOperationException(
                "Agent:ModelDeployment is required for the Foundry provider.");
        }

        if (string.IsNullOrWhiteSpace(_options.TenantId))
        {
            throw new InvalidOperationException(
                "Agent:TenantId is required for browser authentication.");
        }

        var credential = new InteractiveBrowserCredential(
            new InteractiveBrowserCredentialOptions
            {
                TenantId = _options.TenantId,
                RedirectUri = new Uri("http://localhost")
            });

        var projectClient = new AIProjectClient(
            projectEndpoint,
            credential);

        /*
         * This is a code-first agent.
         *
         * The application owns the instructions and orchestration.
         * This does not create a separate portal-managed agent.
         */
        _agent = projectClient.AsAIAgent(
            model: _options.ModelDeployment,
            name: "AgentOpsNexusIncidentAgent",
            instructions: IncidentAgentInstructions.Value);
    }

    public async IAsyncEnumerable<AgentStreamEvent> InvestigateAsync(
        AgentRequestRecord request,
        [EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        /*
         * Preserve the Milestone 1 failure simulation.
         */
        if (request.SimulateFailure)
        {
            throw new HttpRequestException(
                "The simulated downstream AI service is unavailable.");
        }

        yield return CreateEvent(
            "agent.started",
            "Real GenAI investigation started using Microsoft Foundry.");

        yield return CreateEvent(
            "agent.progress",
            "Preparing a bounded incident-analysis request.");

        /*
         * Create a linked token that responds to both:
         *
         * 1. Browser/client cancellation.
         * 2. The configured backend timeout.
         */
        using var timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutSource.CancelAfter(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var stopwatch = Stopwatch.StartNew();
        var prompt = BuildPrompt(request);
        var receivedContent = false;

        /*
         * Obtain the asynchronous enumerator manually.
         *
         * C# does not allow yield return inside a try block that has catch
         * clauses. MoveNextAsync is therefore protected by try/catch, while
         * yield return remains outside it.
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
                hasNext = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogInformation(
                    "Foundry investigation {RequestId} was cancelled by " +
                    "the client after {ElapsedMilliseconds} ms.",
                    request.Id,
                    stopwatch.ElapsedMilliseconds);

                /*
                 * The controller handles this exception and marks the
                 * request as Cancelled.
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
                    $"The GenAI investigation exceeded the configured " +
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
             * Some streaming events may contain only metadata.
             */
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            receivedContent = true;

            /*
             * The controller forwards this event to React through
             * Server-Sent Events.
             */
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
            "Foundry investigation {RequestId} completed using " +
            "deployment {ModelDeployment} in {ElapsedMilliseconds} ms.",
            request.Id,
            _options.ModelDeployment,
            stopwatch.ElapsedMilliseconds);

        yield return CreateEvent(
            "agent.completed",
            $"Real GenAI investigation completed in " +
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

            Important constraints:

            - No telemetry evidence has been supplied in this milestone.
            - Do not claim that any system, deployment, log, trace, metric,
              runbook, database, or source repository was accessed.
            - Treat all root-cause statements as unverified hypotheses.
            - Recommend collecting evidence before consequential action.
            - Do not claim that any production action was executed.
            - Human approval is required before rollback or production change.
            - Clearly separate facts, hypotheses, missing evidence, and
              recommendations.
            - Keep the response concise because the current deployment has a
              limited tokens-per-minute allocation.
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