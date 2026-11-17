using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgentOpsNexus.AgentApi.Data;

public sealed class EfAgentRequestRepository
    : IAgentRequestRepository
{
    private readonly AgentOpsDbContext _dbContext;

    public EfAgentRequestRepository(
        AgentOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentRequestRecord> AddAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _dbContext.AgentRequests.AddAsync(
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
        return _dbContext.AgentRequests
            .Include(request =>
                request.RetriedFromRequest)
            .Include(request =>
                request.RetryAttempts)
            .FirstOrDefaultAsync(
                request =>
                    request.Id == requestId,
                cancellationToken);
    }

    public async Task<
        IReadOnlyCollection<AgentRequestRecord>>
        GetAllAsync(
            CancellationToken cancellationToken)
    {
        /*
         * SQLite cannot translate ORDER BY for DateTimeOffset
         * consistently.
         *
         * Load the records first, then order them in memory.
         * Request history is currently a small development dataset,
         * so this is appropriate for the SQLite milestone.
         *
         * PostgreSQL can perform this ordering at database level later.
         */
        var requests =
            await _dbContext.AgentRequests
                .AsNoTracking()
                .ToArrayAsync(
                    cancellationToken);

        return requests
            .OrderByDescending(
                request => request.CreatedAt)
            .ToArray();
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}