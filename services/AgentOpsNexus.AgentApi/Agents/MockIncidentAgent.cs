using System.Runtime.CompilerServices;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Agents;

public sealed class MockIncidentAgent : IIncidentAgent
{
    public async IAsyncEnumerable<AgentStreamEvent> InvestigateAsync(
        AgentRequestRecord request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return CreateEvent(
            "agent.started",
            $"Investigation started for the {request.Environment} environment.");

        await Task.Delay(600, cancellationToken);

        yield return CreateEvent(
            "agent.progress",
            "Classifying the incident and identifying potentially affected services.");

        await Task.Delay(600, cancellationToken);

        if (request.SimulateFailure)
        {
            throw new HttpRequestException(
                "The simulated downstream AI service is unavailable.");
        }

        yield return CreateEvent(
            "agent.message.delta",
            "The incident has been classified as " +
            $"{request.Severity.ToLowerInvariant()} severity. ");

        await Task.Delay(600, cancellationToken);

        yield return CreateEvent(
            "agent.message.delta",
            "The investigation should correlate recent deployments with error logs, " +
            "distributed traces, service dependencies, and trusted runbooks. ");

        await Task.Delay(600, cancellationToken);

        yield return CreateEvent(
            "agent.message.delta",
            "This first milestone is using a mock agent. " +
            "The next milestone will replace it with Microsoft Agent Framework " +
            "and evidence-grounded retrieval.");

        await Task.Delay(400, cancellationToken);

        yield return CreateEvent(
            "agent.completed",
            "Investigation completed.");
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