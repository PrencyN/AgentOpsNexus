using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;
using AgentOpsNexus.AgentApi.Security;
using FluentAssertions;
using Xunit;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Security;

public sealed class RequestOwnershipTests
    : IClassFixture<
        AgentApiWebApplicationFactory>
{
    private const string UserASubject =
        "ownership-user-a";

    private const string UserAName =
        "Ownership User A";

    private const string UserBSubject =
        "ownership-user-b";

    private const string UserBName =
        "Ownership User B";

    private const string AdministratorSubject =
        "ownership-administrator";

    private const string AdministratorName =
        "Ownership Administrator";

    private readonly
        AgentApiWebApplicationFactory
        _factory;

    public RequestOwnershipTests(
        AgentApiWebApplicationFactory factory)
    {
        _factory =
            factory;
    }

    [Fact]
    public async Task
        Create_AssignsAuthenticatedOwner()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        var requestId =
            await CreateRequestAsync(
                userA,
                "Ownership creation scenario.");

        using var response =
            await userA.GetAsync(
                $"/api/v1/requests/{requestId}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        json.RootElement
            .GetProperty("ownerSubjectId")
            .GetString()
            .Should()
            .Be(UserASubject);

        json.RootElement
            .GetProperty("ownerDisplayName")
            .GetString()
            .Should()
            .Be(UserAName);
    }

    [Fact]
    public async Task
        GetAll_NormalUser_ReturnsOnlyOwnedRequests()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var userB =
            CreateClient(
                UserBSubject,
                UserBName,
                AgentOpsRoles.Investigator);

        var userARequestId =
            await CreateRequestAsync(
                userA,
                "User A isolated history scenario.");

        var userBRequestId =
            await CreateRequestAsync(
                userB,
                "User B isolated history scenario.");

        using var response =
            await userA.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var ids =
            await ReadRequestIdsAsync(
                response);

        ids.Should()
            .Contain(userARequestId);

        ids.Should()
            .NotContain(userBRequestId);
    }

    [Fact]
    public async Task
        GetById_OtherUser_ReturnsNotFound()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var userB =
            CreateClient(
                UserBSubject,
                UserBName,
                AgentOpsRoles.Investigator);

        var requestId =
            await CreateRequestAsync(
                userA,
                "Cross-user read scenario.");

        using var response =
            await userB.GetAsync(
                $"/api/v1/requests/{requestId}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        await AssertRequestNotFoundAsync(
            response);
    }

    [Fact]
    public async Task
        Stream_OtherUser_ReturnsNotFound()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var userB =
            CreateClient(
                UserBSubject,
                UserBName,
                AgentOpsRoles.Investigator);

        var requestId =
            await CreateRequestAsync(
                userA,
                "Cross-user stream scenario.");

        using var response =
            await userB.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        await AssertRequestNotFoundAsync(
            response);
    }

    [Fact]
    public async Task
        Retry_OtherUser_ReturnsNotFound()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var userB =
            CreateClient(
                UserBSubject,
                UserBName,
                AgentOpsRoles.Investigator);

        var requestId =
            await CreateRequestAsync(
                userA,
                "Cross-user retry scenario.",
                simulateFailure:
                    true);

        using var failedStream =
            await userA.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        failedStream.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var failedStreamContent =
            await failedStream.Content
                .ReadAsStringAsync();

        failedStreamContent
            .Should()
            .Contain("agent.failed");

        using var response =
            await userB.PostAsync(
                $"/api/v1/requests/" +
                $"{requestId}/retry",
                content:
                    null);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);

        await AssertRequestNotFoundAsync(
            response);
    }

    [Fact]
    public async Task
        Administrator_GetAll_ReturnsAllOwners()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var userB =
            CreateClient(
                UserBSubject,
                UserBName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var userARequestId =
            await CreateRequestAsync(
                userA,
                "Administrator sees User A.");

        var userBRequestId =
            await CreateRequestAsync(
                userB,
                "Administrator sees User B.");

        using var response =
            await administrator.GetAsync(
                "/api/v1/requests");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var ids =
            await ReadRequestIdsAsync(
                response);

        ids.Should()
            .Contain(userARequestId);

        ids.Should()
            .Contain(userBRequestId);
    }

    [Fact]
    public async Task
        Administrator_GetOtherUsersRequest_ReturnsOk()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var requestId =
            await CreateRequestAsync(
                userA,
                "Administrator cross-owner read.");

        using var response =
            await administrator.GetAsync(
                $"/api/v1/requests/{requestId}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        Administrator_StreamOtherUsersRequest_ReturnsOk()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var requestId =
            await CreateRequestAsync(
                userA,
                "Administrator cross-owner stream.");

        using var response =
            await administrator.GetAsync(
                $"/api/v1/requests/" +
                $"{requestId}/stream");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var streamContent =
            await response.Content
                .ReadAsStringAsync();

        streamContent
            .Should()
            .Contain("agent.completed");
    }

    [Fact]
    public async Task
        AdministratorRetry_PreservesOriginalOwner()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var originalId =
            await CreateRequestAsync(
                userA,
                "Administrator retry ownership scenario.",
                simulateFailure:
                    true);

        using var failedStream =
            await userA.GetAsync(
                $"/api/v1/requests/" +
                $"{originalId}/stream");

        failedStream.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var failedContent =
            await failedStream.Content
                .ReadAsStringAsync();

        failedContent
            .Should()
            .Contain("agent.failed");

        using var retryResponse =
            await administrator.PostAsync(
                $"/api/v1/requests/" +
                $"{originalId}/retry",
                content:
                    null);

        retryResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Accepted);

        var retryId =
            await ReadAcceptedRequestIdAsync(
                retryResponse);

        using var detailsResponse =
            await administrator.GetAsync(
                $"/api/v1/requests/{retryId}");

        detailsResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var detailsJson =
            JsonDocument.Parse(
                await detailsResponse.Content
                    .ReadAsStringAsync());

        detailsJson.RootElement
            .GetProperty("ownerSubjectId")
            .GetString()
            .Should()
            .Be(UserASubject);

        detailsJson.RootElement
            .GetProperty("ownerDisplayName")
            .GetString()
            .Should()
            .Be(UserAName);

        detailsJson.RootElement
            .GetProperty("retriedFromRequestId")
            .GetGuid()
            .Should()
            .Be(originalId);
    }

    [Fact]
    public async Task
        OriginalOwner_CanSeeAdministratorCreatedRetry()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var originalId =
            await CreateRequestAsync(
                userA,
                "Original owner sees admin retry.",
                simulateFailure:
                    true);

        using var failedStream =
            await userA.GetAsync(
                $"/api/v1/requests/" +
                $"{originalId}/stream");

        failedStream.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        using var retryResponse =
            await administrator.PostAsync(
                $"/api/v1/requests/" +
                $"{originalId}/retry",
                content:
                    null);

        retryResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.Accepted);

        var retryId =
            await ReadAcceptedRequestIdAsync(
                retryResponse);

        using var ownerResponse =
            await userA.GetAsync(
                $"/api/v1/requests/{retryId}");

        ownerResponse.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task
        NormalUser_CannotReadAdministratorOwnedRequest()
    {
        using var userA =
            CreateClient(
                UserASubject,
                UserAName,
                AgentOpsRoles.Investigator);

        using var administrator =
            CreateAdministratorClient();

        var administratorRequestId =
            await CreateRequestAsync(
                administrator,
                "Administrator-owned request.");

        using var response =
            await userA.GetAsync(
                $"/api/v1/requests/" +
                $"{administratorRequestId}");

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    private HttpClient
        CreateAdministratorClient()
    {
        return CreateClient(
            AdministratorSubject,
            AdministratorName,
            AgentOpsRoles.Administrator);
    }

    private HttpClient CreateClient(
        string subject,
        string name,
        string role)
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
            TestIdentity.DefaultScope);

        return client;
    }

    private static async Task<Guid>
        CreateRequestAsync(
            HttpClient client,
            string message,
            bool simulateFailure = false)
    {
        using var response =
            await client.PostAsJsonAsync(
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
            .Be(HttpStatusCode.Accepted);

        return await ReadAcceptedRequestIdAsync(
            response);
    }

    private static async Task<Guid>
        ReadAcceptedRequestIdAsync(
            HttpResponseMessage response)
    {
        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .GetProperty("requestId")
            .GetGuid();
    }

    private static async Task<Guid[]>
        ReadRequestIdsAsync(
            HttpResponseMessage response)
    {
        using var json =
            JsonDocument.Parse(
                await response.Content
                    .ReadAsStringAsync());

        return json.RootElement
            .EnumerateArray()
            .Select(
                item =>
                    item
                        .GetProperty("requestId")
                        .GetGuid())
            .ToArray();
    }

    private static async Task
        AssertRequestNotFoundAsync(
            HttpResponseMessage response)
    {
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
}