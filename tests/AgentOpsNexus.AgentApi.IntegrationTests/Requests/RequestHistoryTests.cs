using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Requests;

public sealed class RequestHistoryTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RequestHistoryTests(
        AgentApiWebApplicationFactory factory)
    {
        _client =
            factory.CreateApiClient();
    }

    [Fact]
    public async Task
        GetAll_AfterCreation_ContainsCreatedRequest()
    {
        var requestId =
            await CreateRequestAsync(
                "PaymentService errors increased " +
                "after a deployment.");

        var response =
            await _client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var ids =
            json.RootElement
                .EnumerateArray()
                .Select(
                    item =>
                        item.GetProperty(
                                "requestId")
                            .GetGuid())
                .ToArray();

        ids.Should().Contain(
            requestId);
    }

    [Fact]
    public async Task
        GetById_UnknownRequest_ReturnsNotFound()
    {
        var requestId =
            Guid.NewGuid();

        var response =
            await _client.GetAsync(
                $"/api/v1/requests/{requestId}");

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
        Stream_SimulatedFailure_PersistsRetryableFailure()
    {
        var requestId =
            await CreateRequestAsync(
                message:
                    "PaymentService failed after " +
                    "a recent deployment.",
                simulateFailure:
                    true);

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
            .Contain("agent.failed");

        var details =
            await GetRequestAsync(
                requestId);

        details.GetProperty("status")
            .GetString()
            .Should()
            .Be("Failed");

        details.GetProperty("errorCode")
            .GetString()
            .Should()
            .Be(
                "AGENT_SERVICE_UNAVAILABLE");

        details.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeTrue();

        details.GetProperty("attemptNumber")
            .GetInt32()
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        Retry_FailedRequest_CreatesLinkedSecondAttempt()
    {
        var originalRequestId =
            await CreateRequestAsync(
                message:
                    "Checkout failures followed a " +
                    "PaymentService deployment.",
                simulateFailure:
                    true);

        var failedStream =
            await _client.GetAsync(
                $"/api/v1/requests/" +
                $"{originalRequestId}/stream");

        failedStream.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var failedStreamContent =
            await failedStream.Content
                .ReadAsStringAsync();

        failedStreamContent
            .Should()
            .Contain("agent.failed");

        var retryResponse =
            await _client.PostAsync(
                $"/api/v1/requests/" +
                $"{originalRequestId}/retry",
                content:
                    null);

        retryResponse.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);

        using var retryJson =
            JsonDocument.Parse(
                await retryResponse.Content
                    .ReadAsStringAsync());

        var retryRequestId =
            retryJson.RootElement
                .GetProperty("requestId")
                .GetGuid();

        retryRequestId
            .Should()
            .NotBe(
                originalRequestId);

        var retryDetails =
            await GetRequestAsync(
                retryRequestId);

        retryDetails
            .GetProperty("attemptNumber")
            .GetInt32()
            .Should()
            .Be(2);

        retryDetails
            .GetProperty(
                "retriedFromRequestId")
            .GetGuid()
            .Should()
            .Be(
                originalRequestId);

        var originalDetails =
            await GetRequestAsync(
                originalRequestId);

        originalDetails
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Failed");

        originalDetails
            .GetProperty("attemptNumber")
            .GetInt32()
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task
        Retry_UnknownRequest_ReturnsNotFound()
    {
        var requestId =
            Guid.NewGuid();

        var response =
            await _client.PostAsync(
                $"/api/v1/requests/" +
                $"{requestId}/retry",
                content:
                    null);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task
        GetAll_ReturnsNewestRequestFirst()
    {
        var firstRequestId =
            await CreateRequestAsync(
                "First valid integration incident.");

        await Task.Delay(25);

        var secondRequestId =
            await CreateRequestAsync(
                "Second valid integration incident.");

        var response =
            await _client.GetAsync(
                "/api/v1/requests");

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var ids =
            json.RootElement
                .EnumerateArray()
                .Select(
                    item =>
                        item.GetProperty(
                                "requestId")
                            .GetGuid())
                .ToArray();

        var secondIndex =
            Array.IndexOf(
                ids,
                secondRequestId);

        var firstIndex =
            Array.IndexOf(
                ids,
                firstRequestId);

        secondIndex
            .Should()
            .BeGreaterThanOrEqualTo(0);

        firstIndex
            .Should()
            .BeGreaterThanOrEqualTo(0);

        secondIndex
            .Should()
            .BeLessThan(firstIndex);
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

    private async Task<JsonElement>
        GetRequestAsync(
            Guid requestId)
    {
        var response =
            await _client.GetAsync(
                $"/api/v1/requests/{requestId}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement.Clone();
    }
}