using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Requests;

public sealed class SecurityPipelineTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityPipelineTests(
        AgentApiWebApplicationFactory factory)
    {
        _client =
            factory.CreateApiClient();
    }

    [Fact]
    public async Task
        Health_ReturnsRequiredSecurityHeaders()
    {
        var response =
            await _client.GetAsync(
                "/health");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        GetHeader(
                response,
                "X-Content-Type-Options")
            .Should()
            .Be("nosniff");

        GetHeader(
                response,
                "Referrer-Policy")
            .Should()
            .Be("no-referrer");

        GetHeader(
                response,
                "X-Frame-Options")
            .Should()
            .Be("DENY");

        GetHeader(
                response,
                "Content-Security-Policy")
            .Should()
            .Contain(
                "default-src 'none'");

        GetHeader(
                response,
                "Content-Security-Policy")
            .Should()
            .Contain(
                "frame-ancestors 'none'");

        GetHeader(
                response,
                "Permissions-Policy")
            .Should()
            .Contain("camera=()");

        GetHeader(
                response,
                "Permissions-Policy")
            .Should()
            .Contain("microphone=()");

        GetHeader(
                response,
                "Cache-Control")
            .Should()
            .Contain("no-store");
    }

    [Fact]
    public async Task
        Health_WithoutCorrelationHeader_GeneratesSafeIdentifier()
    {
        var response =
            await _client.GetAsync(
                "/health");

        var correlationId =
            GetHeader(
                response,
                "X-Correlation-ID");

        correlationId
            .Should()
            .MatchRegex(
                "^[a-f0-9]{32}$");
    }

    [Fact]
    public async Task
        Health_WithSafeCorrelationHeader_PreservesIdentifier()
    {
        const string correlationId =
            "integration-safe-001";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/health");

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId);

        var response =
            await _client.SendAsync(request);

        GetHeader(
                response,
                "X-Correlation-ID")
            .Should()
            .Be(correlationId);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        json.RootElement
            .GetProperty("correlationId")
            .GetString()
            .Should()
            .Be(correlationId);
    }

    [Fact]
    public async Task
        Health_WithUnsafeCorrelationHeader_ReplacesIdentifier()
    {
        const string unsafeCorrelationId =
            "invalid correlation id!";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/health");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            unsafeCorrelationId);

        var response =
            await _client.SendAsync(request);

        var returnedCorrelationId =
            GetHeader(
                response,
                "X-Correlation-ID");

        returnedCorrelationId
            .Should()
            .NotBe(
                unsafeCorrelationId);

        returnedCorrelationId
            .Should()
            .MatchRegex(
                "^[a-f0-9]{32}$");
    }

    [Fact]
    public async Task
        Health_HeaderCorrelationId_MatchesBodyCorrelationId()
    {
        var response =
            await _client.GetAsync(
                "/health");

        var headerCorrelationId =
            GetHeader(
                response,
                "X-Correlation-ID");

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        var bodyCorrelationId =
            json.RootElement
                .GetProperty("correlationId")
                .GetString();

        bodyCorrelationId
            .Should()
            .Be(headerCorrelationId);
    }

    [Fact]
    public async Task
        CreateRequests_ExceedFixedWindowLimit_ReturnsTooManyRequests()
    {
        /*
         * Use a separate factory so this rate-limit test
         * cannot consume permits belonging to other test
         * classes.
         */
        await using var factory =
            new AgentApiWebApplicationFactory();

        using var client =
            factory.CreateApiClient();

        HttpResponseMessage? rejectedResponse =
            null;

        for (var index = 0;
             index < 25;
             index++)
        {
            var response =
                await client.PostAsJsonAsync(
                    "/api/v1/requests",
                    new
                    {
                        message =
                            $"Valid integration incident " +
                            $"number {index:00}.",
                        environment =
                            "Development",
                        severity =
                            "High",
                        simulateFailure =
                            false
                    });

            if (response.StatusCode ==
                HttpStatusCode.TooManyRequests)
            {
                rejectedResponse =
                    response;

                break;
            }

            response.Dispose();
        }

        rejectedResponse
            .Should()
            .NotBeNull();

        rejectedResponse!.StatusCode
            .Should()
            .Be(
                HttpStatusCode
                    .TooManyRequests);

        using var json =
            JsonDocument.Parse(
                await rejectedResponse.Content
                    .ReadAsStringAsync());

        json.RootElement
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("RATE_LIMIT_EXCEEDED");

        json.RootElement
            .GetProperty("retryable")
            .GetBoolean()
            .Should()
            .BeTrue();

        rejectedResponse.Dispose();
    }

    private static string GetHeader(
        HttpResponseMessage response,
        string name)
    {
        response.Headers.TryGetValues(
                name,
                out var responseValues)
            .Should()
            .BeTrue(
                $"the response should contain {name}");

        return responseValues!
            .Single();
    }
}