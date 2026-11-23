using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Requests;

public sealed class RequestCreationTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RequestCreationTests(
        AgentApiWebApplicationFactory factory)
    {
        _client =
            factory.CreateApiClient();
    }

    [Fact]
    public async Task
        Create_ValidRequest_ReturnsAcceptedResponse()
    {
        var request =
            CreateRequest(
                message:
                    "Checkout failures increased after " +
                    "the latest PaymentService deployment.");

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);

        response.Headers.Location
            .Should()
            .NotBeNull();

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("requestId")
            .GetGuid()
            .Should()
            .NotBeEmpty();

        root.GetProperty("status")
            .GetString()
            .Should()
            .Be("Queued");

        root.GetProperty("streamUrl")
            .GetString()
            .Should()
            .Contain("/stream");
    }

    [Fact]
    public async Task
        Create_ValidRequest_ReturnsCorrelationId()
    {
        const string correlationId =
            "integration-create-001";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/requests");

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId);

        request.Content =
            JsonContent.Create(
                CreateRequest(
                    message:
                        "PaymentService returned increased " +
                        "errors after deployment."));

        var response =
            await _client.SendAsync(request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);

        response.Headers
            .GetValues(
                "X-Correlation-ID")
            .Single()
            .Should()
            .Be(correlationId);
    }

    [Fact]
    public async Task
        Create_MessageBelowMinimumLength_ReturnsModelValidationFailure()
    {
        var request =
            CreateRequest(
                message:
                    "Failure");

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "REQUEST_VALIDATION_FAILED");

        root.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeFalse();

        root.GetProperty("correlationId")
            .GetString()
            .Should()
            .NotBeNullOrWhiteSpace();

        root.GetProperty("errors")
            .EnumerateObject()
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task
        Create_EmptyMessage_ReturnsModelValidationFailure()
    {
        var request =
            CreateRequest(
                message:
                    string.Empty);

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "REQUEST_VALIDATION_FAILED");

        root.GetProperty("errors")
            .EnumerateObject()
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task
        Create_NullByteInMessage_ReturnsInputPolicyFailure()
    {
        var request =
            CreateRequest(
                message:
                    "Invalid\0incident description");

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "INCIDENT_DESCRIPTION_NULL_BYTE");

        root.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task
        Create_UnsupportedControlCharacter_ReturnsInputPolicyFailure()
    {
        var request =
            CreateRequest(
                message:
                    "Checkout failed after deployment." +
                    '\u0007');

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "INCIDENT_DESCRIPTION_CONTROL_CHARACTER");

        root.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task
        Create_MessageOverMaximumLength_ReturnsModelValidationFailure()
    {
        var request =
            CreateRequest(
                message:
                    new string(
                        'A',
                        4001));

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.BadRequest);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var root =
            json.RootElement;

        root.GetProperty("code")
            .GetString()
            .Should()
            .Be(
                "REQUEST_VALIDATION_FAILED");

        root.GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeFalse();

        root.GetProperty("errors")
            .EnumerateObject()
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task
        Create_WindowsLineEndings_NormalizesPersistedMessage()
    {
        var request =
            CreateRequest(
                message:
                    "Checkout failed.\r\n" +
                    "Payment provider timed out.");

        var response =
            await _client.PostAsJsonAsync(
                "/api/v1/requests",
                request);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);

        using var acceptedJson =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var requestId =
            acceptedJson.RootElement
                .GetProperty("requestId")
                .GetGuid();

        var detailsResponse =
            await _client.GetAsync(
                $"/api/v1/requests/{requestId}");

        detailsResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var detailsJson =
            JsonDocument.Parse(
                await detailsResponse.Content
                    .ReadAsStringAsync());

        detailsJson.RootElement
            .GetProperty("message")
            .GetString()
            .Should()
            .Be(
                "Checkout failed.\n" +
                "Payment provider timed out.");
    }

    private static object CreateRequest(
        string message,
        bool simulateFailure = false)
    {
        return new
        {
            message,
            environment =
                "Development",
            severity =
                "High",
            simulateFailure
        };
    }
}