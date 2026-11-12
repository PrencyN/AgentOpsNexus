using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public interface IAgentRequestRepository
{
    AgentRequestRecord Add(AgentRequestRecord request);

    AgentRequestRecord? Get(Guid requestId);

    IReadOnlyCollection<AgentRequestRecord> GetAll();
}