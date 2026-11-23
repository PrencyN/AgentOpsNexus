using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Requests;

public sealed class StreamingTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StreamingTests(
        AgentApiWebApplicationFactory factory)
    {
        _client =
            factory.CreateApiClient();
    }

    [Fact]
    public async Task
        Stream_ValidRequest_ReturnsEventStream()
    {
        var requestId =
            await CreateRequestAsync(
                "PaymentService failures increased " +
                "after deployment.");

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "text/event-stream"));

        var response =
            await _client.SendAsync(
                request,
                HttpCompletionOption
                    .ResponseHeadersRead);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        response.Content.Headers
            .ContentType?
            .MediaType
            .Should()
            .Be("text/event-stream");

        var content =
            await response.Content
                .ReadAsStringAsync();

        content.Should().Contain(
            "event: agent.started");

        content.Should().Contain(
            "event: agent.progress");

        content.Should().Contain(
            "event: agent.message.delta");

        content.Should().Contain(
            "event: agent.completed");
    }

    [Fact]
    public async Task
        Stream_ValidRequest_PersistsCompletedState()
    {
        var requestId =
            await CreateRequestAsync(
                "CheckoutService returned increased " +
                "errors after deployment.");

        var streamResponse =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        streamResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var streamContent =
            await streamResponse.Content
                .ReadAsStringAsync();

        streamContent
            .Should()
            .Contain("agent.completed");

        var detailsResponse =
            await _client.GetAsync(
                $"/api/v1/requests/{requestId}");

        detailsResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            JsonDocument.Parse(
                await detailsResponse.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("status")
            .GetString()
            .Should()
            .Be("Completed");

        root.GetProperty("response")
            .GetString()
            .Should()
            .NotBeNullOrWhiteSpace();

        root.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task
        Stream_SimulatedFailure_ReturnsFailedEvent()
    {
        var requestId =
            await CreateRequestAsync(
                message:
                    "PaymentService is unavailable after " +
                    "a deployment.",
                simulateFailure:
                    true);

        var response =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var content =
            await response.Content
                .ReadAsStringAsync();

        content.Should().Contain(
            "event: agent.failed");

        content.Should().Contain(
            "temporarily unavailable");
    }

    [Fact]
    public async Task
        Stream_UnknownRequest_ReturnsNotFound()
    {
        var requestId =
            Guid.NewGuid();

        var response =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.NotFound);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        json.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("REQUEST_NOT_FOUND");
    }

    [Fact]
    public async Task
        Stream_CompletedRequestSecondAttempt_ReturnsConflict()
    {
        var requestId =
            await CreateRequestAsync(
                "PaymentService latency increased " +
                "after deployment.");

        var firstResponse =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        firstResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var firstContent =
            await firstResponse.Content
                .ReadAsStringAsync();

        firstContent.Should().Contain(
            "agent.completed");

        var secondResponse =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        secondResponse.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Conflict);

        using var json =
            JsonDocument.Parse(
                await secondResponse.Content
                    .ReadAsStringAsync());

        json.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "REQUEST_ALREADY_COMPLETED");
    }

    private async Task<Guid>
        CreateRequestAsync(
            string message,
            bool simulateFailure = false)
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                new
                {
                    message,
                    environment =
                        "Development",
                    severity =
                        "High",
                    simulateFailure
                });

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .GetProperty("requestId")
            .GetGuid();
    }
}