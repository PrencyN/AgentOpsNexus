using System.Collections.Concurrent;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public sealed class InMemoryAgentRequestRepository
    : IAgentRequestRepository
{
    private readonly ConcurrentDictionary<
        Guid,
        AgentRequestRecord> _requests = new();

    public Task<AgentRequestRecord> AddAsync(
        AgentRequestRecord request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_requests.TryAdd(
                request.Id,
                request))
        {
            throw new InvalidOperationException(
                $"Request {request.Id} already exists.");
        }

        return Task.FromResult(request);
    }

    public Task<AgentRequestRecord?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _requests.TryGetValue(
            requestId,
            out var request);

        return Task.FromResult(request);
    }

    public Task<IReadOnlyCollection<AgentRequestRecord>>
        GetAllAsync(
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<AgentRequestRecord> result =
            _requests.Values
                .OrderByDescending(
                    item => item.CreatedAt)
                .ToArray();

        return Task.FromResult(result);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        /*
         * Objects are stored by reference, so domain mutations
         * are already reflected in memory.
         */
        return Task.CompletedTask;
    }
}