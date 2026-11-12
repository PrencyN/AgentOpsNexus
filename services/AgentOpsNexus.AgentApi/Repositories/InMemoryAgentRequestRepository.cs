using System.Collections.Concurrent;
using AgentOpsNexus.AgentApi.Domain;

namespace AgentOpsNexus.AgentApi.Repositories;

public sealed class InMemoryAgentRequestRepository : IAgentRequestRepository
{
    private readonly ConcurrentDictionary<Guid, AgentRequestRecord> _requests = new();

    public AgentRequestRecord Add(AgentRequestRecord request)
    {
        if (!_requests.TryAdd(request.Id, request))
        {
            throw new InvalidOperationException(
                $"Request {request.Id} already exists.");
        }

        return request;
    }

    public AgentRequestRecord? Get(Guid requestId)
    {
        return _requests.GetValueOrDefault(requestId);
    }

    public IReadOnlyCollection<AgentRequestRecord> GetAll()
    {
        return _requests.Values
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();
    }
}