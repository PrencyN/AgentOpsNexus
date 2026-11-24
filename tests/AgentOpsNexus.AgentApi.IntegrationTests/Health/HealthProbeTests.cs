using System.Net;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Health;

public sealed class HealthProbeTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly HttpClient
        _client;

    public HealthProbeTests(
        AgentApiWebApplicationFactory factory)
    {
        _client =
            factory.CreateApiClient();
    }

    [Fact]
    public async Task
        Live_ReturnsHealthy()
    {
        using var response =
            await _client.GetAsync(
                "/health/live");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        json.RootElement
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }

    [Fact]
    public async Task
        Live_ContainsNoDependencyChecks()
    {
        using var response =
            await _client.GetAsync(
                "/health/live");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        var checks =
            json.RootElement
                .GetProperty("checks");

        checks.ValueKind
            .Should()
            .Be(JsonValueKind.Array);

        checks.GetArrayLength()
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task
        Live_AllowsAnonymousAccess()
    {
        using var response =
            await _client.GetAsync(
                "/health/live");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Ready_ReturnsHealthy()
    {
        using var response =
            await _client.GetAsync(
                "/health/ready");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        json.RootElement
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }

    [Fact]
    public async Task
        Ready_ContainsHealthyDatabaseCheck()
    {
        using var response =
            await _client.GetAsync(
                "/health/ready");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        var databaseCheck =
            FindCheck(
                json.RootElement,
                "agentops-database");

        databaseCheck
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");

        databaseCheck
            .GetProperty("description")
            .GetString()
            .Should()
            .Be(
                "The AgentOps database " +
                "is reachable.");
    }

    [Fact]
    public async Task
        Ready_AllowsAnonymousAccess()
    {
        using var response =
            await _client.GetAsync(
                "/health/ready");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        LegacyHealth_ReturnsHealthy()
    {
        using var response =
            await _client.GetAsync(
                "/health");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        json.RootElement
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }

    [Fact]
    public async Task
        LegacyHealth_ContainsDatabaseCheck()
    {
        using var response =
            await _client.GetAsync(
                "/health");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            await ReadJsonAsync(
                response);

        var databaseCheck =
            FindCheck(
                json.RootElement,
                "agentops-database");

        databaseCheck
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Healthy");
    }

    private static async Task<JsonDocument>
        ReadJsonAsync(
            HttpResponseMessage response)
    {
        var content =
            await response.Content
                .ReadAsStringAsync();

        content
            .Should()
            .NotBeNullOrWhiteSpace();

        return JsonDocument.Parse(
            content);
    }

    private static JsonElement FindCheck(
        JsonElement root,
        string checkName)
    {
        var checks =
            root.GetProperty(
                "checks");

        checks.ValueKind
            .Should()
            .Be(JsonValueKind.Array);

        foreach (
            var check in
                checks.EnumerateArray())
        {
            if (string.Equals(
                    check
                        .GetProperty("name")
                        .GetString(),
                    checkName,
                    StringComparison.Ordinal))
            {
                return check;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Health check '{checkName}' " +
            "was not returned.");
    }
}
