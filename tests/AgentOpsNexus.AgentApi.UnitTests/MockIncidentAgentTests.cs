using AgentOpsNexus.AgentApi.Agents;
using AgentOpsNexus.AgentApi.Contracts;
using AgentOpsNexus.AgentApi.Domain;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class MockIncidentAgentTests
{
    [Fact]
    public async Task
        InvestigateAsync_ValidRequest_EmitsExpectedEvents()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: false);

        var updates =
            new List<AgentStreamEvent>();

        await foreach (
            var update in
                agent.InvestigateAsync(
                    request,
                    CancellationToken.None))
        {
            updates.Add(update);
        }

        updates.Should().NotBeEmpty();

        updates
            .Select(update => update.Type)
            .Should()
            .Contain("agent.started");

        updates
            .Select(update => update.Type)
            .Should()
            .Contain("agent.progress");

        updates
            .Select(update => update.Type)
            .Should()
            .Contain("agent.message.delta");

        updates
            .Select(update => update.Type)
            .Should()
            .Contain("agent.completed");
    }

    [Fact]
    public async Task
        InvestigateAsync_ValidRequest_StartsBeforeCompletion()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: false);

        var eventTypes =
            new List<string>();

        await foreach (
            var update in
                agent.InvestigateAsync(
                    request,
                    CancellationToken.None))
        {
            eventTypes.Add(update.Type);
        }

        eventTypes.First().Should().Be(
            "agent.started");

        eventTypes.Last().Should().Be(
            "agent.completed");
    }

    [Fact]
    public async Task
        InvestigateAsync_ValidRequest_ProducesResponseContent()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: false);

        var responseContent =
            new List<string>();

        await foreach (
            var update in
                agent.InvestigateAsync(
                    request,
                    CancellationToken.None))
        {
            if (update.Type ==
                "agent.message.delta")
            {
                responseContent.Add(
                    update.Message);
            }
        }

        responseContent.Should().NotBeEmpty();

        string.Join(
                string.Empty,
                responseContent)
            .Should()
            .Contain(
                "deployment");
    }

    [Fact]
    public async Task
        InvestigateAsync_SimulatedFailure_ThrowsHttpRequestException()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: true);

        var action =
            async () =>
            {
                await foreach (
                    var _ in
                        agent.InvestigateAsync(
                            request,
                            CancellationToken.None))
                {
                }
            };

        await action
            .Should()
            .ThrowAsync<HttpRequestException>()
            .WithMessage(
                "*simulated downstream AI service*");
    }

    [Fact]
    public async Task
        InvestigateAsync_CancelledToken_ThrowsCancellation()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: false);

        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        var action =
            async () =>
            {
                await foreach (
                    var _ in
                        agent.InvestigateAsync(
                            request,
                            cancellationSource.Token))
                {
                }
            };

        await action
            .Should()
            .ThrowAsync<
                OperationCanceledException>();
    }

    [Fact]
    public async Task
        InvestigateAsync_EventsContainTimestamps()
    {
        var agent =
            new MockIncidentAgent();

        var request =
            CreateRequest(
                simulateFailure: false);

        var before =
            DateTimeOffset.UtcNow
                .AddSeconds(-1);

        var updates =
            new List<AgentStreamEvent>();

        await foreach (
            var update in
                agent.InvestigateAsync(
                    request,
                    CancellationToken.None))
        {
            updates.Add(update);
        }

        updates.Should().OnlyContain(
            update =>
                update.Timestamp >= before);
    }

    private static AgentRequestRecord CreateRequest(
        bool simulateFailure)
    {
        return new AgentRequestRecord(
            id: Guid.NewGuid(),
            message:
                "Checkout failures increased " +
                "after deployment.",
            environment:
                "Development",
            severity:
                "High",
            simulateFailure:
                simulateFailure,
            createdAt:
                DateTimeOffset.UtcNow,
            correlationId:
                Guid.NewGuid().ToString("N"));
    }
}