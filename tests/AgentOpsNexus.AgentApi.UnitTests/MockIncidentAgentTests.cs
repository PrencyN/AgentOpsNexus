using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Domain;
using FluentAssertions;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class MockIncidentAgentTests
{
    [Fact]
    public async Task InvestigateAsync_ValidRequest_EmitsStartAndCompletion()
    {
        var agent = new MockIncidentAgent();

        var request = CreateRequest(simulateFailure: false);

        var events = new List<string>();

        await foreach (var update in agent.InvestigateAsync(
                           request,
                           CancellationToken.None))
        {
            events.Add(update.Type);
        }

        events.Should().Contain("agent.started");
        events.Should().Contain("agent.message.delta");
        events.Should().Contain("agent.completed");
    }

    [Fact]
    public async Task InvestigateAsync_SimulatedFailure_ThrowsException()
    {
        var agent = new MockIncidentAgent();

        var request = CreateRequest(simulateFailure: true);

        var action = async () =>
        {
            await foreach (var _ in agent.InvestigateAsync(
                               request,
                               CancellationToken.None))
            {
            }
        };

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    private static AgentRequestRecord CreateRequest(bool simulateFailure)
    {
        return new AgentRequestRecord
        {
            Id = Guid.NewGuid(),
            Message = "Checkout failures increased after deployment.",
            Environment = "Development",
            Severity = "High",
            SimulateFailure = simulateFailure,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}