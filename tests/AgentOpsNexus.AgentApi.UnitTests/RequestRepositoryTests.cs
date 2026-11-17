using AgentOpsNexus.AgentApi.Domain;
using AgentOpsNexus.AgentApi.Repositories;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class RequestRepositoryTests
{
    [Fact]
    public async Task
        AddAsync_ValidRequest_CanBeRetrieved()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        var request = CreateRequest();

        await repository.AddAsync(
            request,
            CancellationToken.None);

        var stored =
            await repository.GetAsync(
                request.Id,
                CancellationToken.None);

        stored.Should().NotBeNull();

        stored!.Id.Should().Be(request.Id);

        stored.Message.Should().Be(
            request.Message);

        stored.Environment.Should().Be(
            "Development");

        stored.Severity.Should().Be(
            "High");

        stored.Status.Should().Be(
            AgentRequestStatus.Queued);
    }

    [Fact]
    public async Task
        AddAsync_DuplicateIdentifier_ThrowsException()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        var request = CreateRequest();

        await repository.AddAsync(
            request,
            CancellationToken.None);

        var action =
            async () =>
                await repository.AddAsync(
                    request,
                    CancellationToken.None);

        await action
            .Should()
            .ThrowAsync<
                InvalidOperationException>()
            .WithMessage(
                $"*{request.Id}*already exists*");
    }

    [Fact]
    public async Task
        GetAsync_UnknownIdentifier_ReturnsNull()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        var result =
            await repository.GetAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task
        GetAllAsync_ReturnsNewestRequestFirst()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        var olderRequest =
            CreateRequest(
                createdAt:
                    DateTimeOffset.UtcNow
                        .AddMinutes(-10));

        var newerRequest =
            CreateRequest(
                createdAt:
                    DateTimeOffset.UtcNow);

        await repository.AddAsync(
            olderRequest,
            CancellationToken.None);

        await repository.AddAsync(
            newerRequest,
            CancellationToken.None);

        var requests =
            await repository.GetAllAsync(
                CancellationToken.None);

        requests.Should().HaveCount(2);

        requests.First().Id.Should().Be(
            newerRequest.Id);

        requests.Last().Id.Should().Be(
            olderRequest.Id);
    }

    [Fact]
    public async Task
        SaveChangesAsync_PreservesDomainMutation()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        var request = CreateRequest();

        await repository.AddAsync(
            request,
            CancellationToken.None);

        request.MarkProcessing();
        request.AppendResponse(
            "Investigation result.");
        request.MarkCompleted();

        await repository.SaveChangesAsync(
            CancellationToken.None);

        var stored =
            await repository.GetAsync(
                request.Id,
                CancellationToken.None);

        stored.Should().NotBeNull();

        stored!.Status.Should().Be(
            AgentRequestStatus.Completed);

        stored.Response.Should().Be(
            "Investigation result.");

        stored.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void
        MarkFailed_RetryableFailure_SetsExpectedState()
    {
        var request = CreateRequest();

        request.MarkFailed(
            "AGENT_SERVICE_UNAVAILABLE",
            retryable: true);

        request.Status.Should().Be(
            AgentRequestStatus.Failed);

        request.ErrorCode.Should().Be(
            "AGENT_SERVICE_UNAVAILABLE");

        request.Retryable.Should().BeTrue();

        request.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void
        MarkCancelled_SetsRetryableCancelledState()
    {
        var request = CreateRequest();

        request.MarkCancelled();

        request.Status.Should().Be(
            AgentRequestStatus.Cancelled);

        request.ErrorCode.Should().Be(
            "REQUEST_CANCELLED");

        request.Retryable.Should().BeTrue();

        request.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void
        Constructor_TrimsTextAndStoresCorrelationId()
    {
        var correlationId =
            Guid.NewGuid().ToString("N");

        var request =
            new AgentRequestRecord(
                id: Guid.NewGuid(),
                message:
                    "  Investigate checkout failures.  ",
                environment:
                    "  Development  ",
                severity:
                    "  High  ",
                simulateFailure: false,
                createdAt:
                    DateTimeOffset.UtcNow,
                correlationId:
                    correlationId);

        request.Message.Should().Be(
            "Investigate checkout failures.");

        request.Environment.Should().Be(
            "Development");

        request.Severity.Should().Be(
            "High");

        request.CorrelationId.Should().Be(
            correlationId);
    }

    [Fact]
    public async Task
        RepositoryMethods_CancelledToken_ThrowCancellation()
    {
        var repository =
            new InMemoryAgentRequestRepository();

        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        var addAction =
            async () =>
                await repository.AddAsync(
                    CreateRequest(),
                    cancellationSource.Token);

        await addAction
            .Should()
            .ThrowAsync<
                OperationCanceledException>();
    }

    private static AgentRequestRecord CreateRequest(
        DateTimeOffset? createdAt = null)
    {
        return new AgentRequestRecord(
            id: Guid.NewGuid(),
            message:
                "Investigate checkout failures " +
                "after deployment.",
            environment:
                "Development",
            severity:
                "High",
            simulateFailure:
                false,
            createdAt:
                createdAt
                ??
                DateTimeOffset.UtcNow,
            correlationId:
                Guid.NewGuid().ToString("N"));
    }
}