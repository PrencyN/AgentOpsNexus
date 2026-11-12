using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Agents;

public interface IIncidentAgent
{
    IAsyncEnumerable<AgentStreamEvent> InvestigateAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken);
}