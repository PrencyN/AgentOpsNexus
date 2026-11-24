using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public interface IAgentRequestRepository
{
    Task<AgentRequestRecord> AddAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken);

    /*
     * Compatibility method for domain and repository
     * tests. Production controller code should use the
     * owner-aware overload.
     */
    Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken);

    Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        string ownerSubjectId,
        bool includeAllOwners,
        CancellationToken cancellationToken);

    /*
     * Compatibility method for current tests.
     * Production controller code should use the
     * owner-aware overload.
     */
    Task<IReadOnlyCollection<
        AgentRequestRecord>>
        GetAllAsync(
            CancellationToken cancellationToken);

    Task<IReadOnlyCollection<
        AgentRequestRecord>>
        GetAllAsync(
            string ownerSubjectId,
            bool includeAllOwners,
            CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}