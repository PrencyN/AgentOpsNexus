using System.Collections.Concurrent;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public sealed class
    InMemoryAgentRequestRepository
    : IAgentRequestRepository
{
    private readonly ConcurrentDictionary<
        Guid,
        AgentRequestRecord>
        _requests =
            new();

    public Task<AgentRequestRecord> AddAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(
            request);

        if (!_requests.TryAdd(
                request.Id,
                request))
        {
            throw new InvalidOperationException(
                $"Request {request.Id} already " +
                "exists.");
        }

        return Task.FromResult(
            request);
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
        cancellationToken
            .ThrowIfCancellationRequested();

        _requests.TryGetValue(
            requestId,
            out var request);

        if (request is null)
        {
            return Task.FromResult<
                AgentRequestRecord?>(
                    null);
        }

        if (!includeAllOwners &&
            !string.Equals(
                request.OwnerSubjectId,
                ownerSubjectId,
                StringComparison.Ordinal))
        {
            return Task.FromResult<
                AgentRequestRecord?>(
                    null);
        }

        return Task.FromResult<
            AgentRequestRecord?>(
                request);
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

    public Task<IReadOnlyCollection<
        AgentRequestRecord>>
        GetAllAsync(
            string ownerSubjectId,
            bool includeAllOwners,
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        IEnumerable<AgentRequestRecord>
            query =
                _requests.Values;

        if (!includeAllOwners)
        {
            query =
                query.Where(
                    request =>
                        string.Equals(
                            request.OwnerSubjectId,
                            ownerSubjectId,
                            StringComparison.Ordinal));
        }

        IReadOnlyCollection<
            AgentRequestRecord>
            result =
                query
                    .OrderByDescending(
                        request =>
                            request.CreatedAt)
                    .ToArray();

        return Task.FromResult(
            result);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        /*
         * Objects are stored by reference, so domain
         * mutations are already reflected in memory.
         */
        return Task.CompletedTask;
    }
}