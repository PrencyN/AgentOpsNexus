using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public interface IAgentRequestRepository
{
    Task<AgentRequestRecord> AddAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken);

    Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AgentRequestRecord>>
        GetAllAsync(
            CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}