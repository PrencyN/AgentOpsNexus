using System.Net;
using System.Net.Http.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using AgentOpsNexus.AgentApi.Security;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Security;

public sealed class AuthorizationTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private readonly
        AgentApiWebApplicationFactory
        _factory;

    public AuthorizationTests(
        AgentApiWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    [Fact]
    public async Task
        AnonymousUser_GetRequestHistory_ReturnsUnauthorized()
    {
        using var client =
            CreateClient(
                subject:
                    TestAuthenticationHandler
                        .AnonymousSubject);

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        AnonymousUser_CreateRequest_ReturnsUnauthorized()
    {
        using var client =
            CreateClient(
                subject:
                    TestAuthenticationHandler
                        .AnonymousSubject);

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        Reader_GetRequestHistory_ReturnsOk()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Reader);

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Reader_CreateRequest_ReturnsForbidden()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Reader);

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        Investigator_GetRequestHistory_ReturnsOk()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Investigator);

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Investigator_CreateRequest_ReturnsAccepted()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Investigator);

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task
        Approver_GetRequestHistory_ReturnsOk()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Approver);

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Approver_CreateRequest_ReturnsForbidden()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Approver);

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        Administrator_GetRequestHistory_ReturnsOk()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Administrator);

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Administrator_CreateRequest_ReturnsAccepted()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Administrator);

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task
        UnknownRole_GetRequestHistory_ReturnsForbidden()
    {
        using var client =
            CreateClient(
                role:
                    "Incident.Unknown");

        var response =
            await client.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        UnknownRole_CreateRequest_ReturnsForbidden()
    {
        using var client =
            CreateClient(
                role:
                    "Incident.Unknown");

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        AnonymousUser_RootEndpoint_ReturnsOk()
    {
        using var client =
            CreateClient(
                subject:
                    TestAuthenticationHandler
                        .AnonymousSubject);

        var response =
            await client.GetAsync(
                "/");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        AnonymousUser_HealthEndpoint_ReturnsOk()
    {
        using var client =
            CreateClient(
                subject:
                    TestAuthenticationHandler
                        .AnonymousSubject);

        var response =
            await client.GetAsync(
                "/health");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        MultipleAllowedRoles_CreateRequest_ReturnsAccepted()
    {
        using var client =
            CreateClient(
                role:
                    string.Join(
                        ",",
                        AgentOpsRoles.Reader,
                        AgentOpsRoles.Investigator));

        var response =
            await PostValidRequestAsync(
                client);

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task
        Reader_GetUnknownRequest_ReturnsNotFound()
    {
        using var client =
            CreateClient(
                role:
                    AgentOpsRoles.Reader);

        var response =
            await client.GetAsync(
                $"/api/v1/requests/" +
                $"{Guid.NewGuid()}");

        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient(
        string role =
            AgentOpsRoles.Investigator,

        string subject =
            TestIdentity.DefaultSubject,

        string name =
            TestIdentity.DefaultName,

        string scope =
            TestIdentity.DefaultScope)
    {
        var client =
            _factory.CreateApiClient();

        client.DefaultRequestHeaders.Add(
            TestIdentity.SubjectHeader,
            subject);

        client.DefaultRequestHeaders.Add(
            TestIdentity.NameHeader,
            name);

        client.DefaultRequestHeaders.Add(
            TestIdentity.RolesHeader,
            role);

        client.DefaultRequestHeaders.Add(
            TestIdentity.ScopeHeader,
            scope);

        return client;
    }

    private static async Task<
        HttpResponseMessage>
        PostValidRequestAsync(
            HttpClient client)
    {
        var request =
            new
            {
                message =
                    "Checkout failures increased " +
                    "after a recent deployment.",

                environment =
                    "Development",

                severity =
                    "High",

                simulateFailure =
                    false
            };

        return await client.PostAsJsonAsync(
            "/api/v1/requests",
            request);
    }
}