using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgentOpsNexus.AgentApi.Data;

public sealed class EfAgentRequestRepository
    : IAgentRequestRepository
{
    private readonly AgentOpsDbContext
        _dbContext;

    public EfAgentRequestRepository(
        AgentOpsDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public async Task<AgentRequestRecord>
        AddAsync(
            AgentRequestRecord request,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        await _dbContext.AgentRequests
            .AddAsync(
                request,
                cancellationToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return request;
    }

    public Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        return GetAsync(
            requestId,
            ownerSubjectId:
                string.Empty,
            includeAllOwners:
                true,
            cancellationToken);
    }

    public Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        string ownerSubjectId,
        bool includeAllOwners,
        CancellationToken cancellationToken)
    {
        IQueryable<AgentRequestRecord>
            query =
                _dbContext.AgentRequests
                    .Include(
                        request =>
                            request
                                .RetriedFromRequest)
                    .Include(
                        request =>
                            request
                                .RetryAttempts);

        if (!includeAllOwners)
        {
            query =
                query.Where(
                    request =>
                        request.OwnerSubjectId ==
                        ownerSubjectId);
        }

        return query.FirstOrDefaultAsync(
            request =>
                request.Id == requestId,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<
        AgentRequestRecord>>
        GetAllAsync(
            CancellationToken cancellationToken)
    {
        return GetAllAsync(
            ownerSubjectId:
                string.Empty,
            includeAllOwners:
                true,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<
        AgentRequestRecord>>
        GetAllAsync(
            string ownerSubjectId,
            bool includeAllOwners,
            CancellationToken cancellationToken)
    {
        IQueryable<AgentRequestRecord>
            query =
                _dbContext.AgentRequests
                    .AsNoTracking();

        if (!includeAllOwners)
        {
            query =
                query.Where(
                    request =>
                        request.OwnerSubjectId ==
                        ownerSubjectId);
        }

        /*
         * SQLite cannot translate ORDER BY for
         * DateTimeOffset consistently.
         *
         * Load the filtered records first, then order
         * them in memory. PostgreSQL can perform this
         * ordering at database level later.
         */
        var requests =
            await query.ToArrayAsync(
                cancellationToken);

        return requests
            .OrderByDescending(
                request =>
                    request.CreatedAt)
            .ToArray();
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}