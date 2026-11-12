using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using FluentAssertions;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class RequestRepositoryTests
{
    [Fact]
    public void Add_ValidRequest_CanBeRetrieved()
    {
        var repository = new InMemoryAgentRequestRepository();

        var request = CreateRequest();

        repository.Add(request);

        var stored = repository.Get(request.Id);

        stored.Should().NotBeNull();
        stored!.Message.Should().Be(request.Message);
    }

    [Fact]
    public void Add_DuplicateIdentifier_ThrowsException()
    {
        var repository = new InMemoryAgentRequestRepository();

        var request = CreateRequest();

        repository.Add(request);

        var action = () => repository.Add(request);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_RetryableFailure_SetsExpectedState()
    {
        var request = CreateRequest();

        request.MarkFailed(
            "AGENT_SERVICE_UNAVAILABLE",
            retryable: true);

        request.Status.Should().Be(AgentRequestStatus.Failed);
        request.ErrorCode.Should().Be("AGENT_SERVICE_UNAVAILABLE");
        request.Retryable.Should().BeTrue();
        request.CompletedAt.Should().NotBeNull();
    }

    private static AgentRequestRecord CreateRequest()
    {
        return new AgentRequestRecord
        {
            Id = Guid.NewGuid(),
            Message = "Investigate checkout failures after deployment.",
            Environment = "Development",
            Severity = "High",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}