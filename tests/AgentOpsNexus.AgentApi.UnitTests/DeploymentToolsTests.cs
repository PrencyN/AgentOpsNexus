using System.Text.Json;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class DeploymentToolsTests
{
    private readonly DeploymentTools _tool;

    public DeploymentToolsTests()
    {
        _tool = new DeploymentTools(
            new MockDeploymentEvidenceService(),
            NullLogger<DeploymentTools>.Instance);
    }

    [Fact]
    public async Task
        GetRecentDeployments_ValidPaymentServiceRequest_ReturnsEvidence()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: "PaymentService",
                environment: "Development",
                lookbackHours: 24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        root
            .GetProperty("tool")
            .GetString()
            .Should()
            .Be("GetRecentDeployments");

        root
            .GetProperty("serviceName")
            .GetString()
            .Should()
            .Be("PaymentService");

        root
            .GetProperty("environment")
            .GetString()
            .Should()
            .Be("Development");

        root
            .GetProperty("evidenceCount")
            .GetInt32()
            .Should()
            .Be(2);

        var evidenceIds =
            root
                .GetProperty("evidence")
                .EnumerateArray()
                .Select(
                    item =>
                        item
                            .GetProperty("evidenceId")
                            .GetString())
                .ToArray();

        evidenceIds
            .Should()
            .Contain("DEP-EV-001");

        evidenceIds
            .Should()
            .Contain("DEP-EV-002");
    }

    [Fact]
    public async Task
        GetRecentDeployments_InputContainingWhitespace_IsNormalized()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: " PaymentService ",
                environment: " Development ",
                lookbackHours: 24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        root
            .GetProperty("serviceName")
            .GetString()
            .Should()
            .Be("PaymentService");

        root
            .GetProperty("environment")
            .GetString()
            .Should()
            .Be("Development");

        root
            .GetProperty("evidenceCount")
            .GetInt32()
            .Should()
            .Be(2);
    }

    [Fact]
    public async Task
        GetRecentDeployments_InvalidEnvironment_ReturnsValidationError()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: "PaymentService",
                environment: "Unknown",
                lookbackHours: 24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        root
            .GetProperty("error")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("INVALID_ENVIRONMENT");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    [InlineData(500)]
    public async Task
        GetRecentDeployments_InvalidLookback_ReturnsValidationError(
            int invalidLookback)
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: "PaymentService",
                environment: "Development",
                lookbackHours: invalidLookback,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        root
            .GetProperty("error")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("INVALID_LOOKBACK");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public async Task
        GetRecentDeployments_InvalidServiceName_ReturnsValidationError(
            string invalidServiceName)
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: invalidServiceName,
                environment: "Development",
                lookbackHours: 24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        root
            .GetProperty("error")
            .GetProperty("code")
            .GetString()
            .Should()
            .Be("INVALID_SERVICE_NAME");
    }

    [Fact]
    public async Task
        GetRecentDeployments_UnknownService_ReturnsEmptyEvidence()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: "UnknownService",
                environment: "Development",
                lookbackHours: 24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

        root
            .GetProperty("evidenceCount")
            .GetInt32()
            .Should()
            .Be(0);

        root
            .GetProperty("evidence")
            .GetArrayLength()
            .Should()
            .Be(0);

        root
            .GetProperty("limitations")
            .GetString()
            .Should()
            .Contain(
                "Do not infer that no deployment occurred");
    }

    [Fact]
    public async Task
        GetRecentDeployments_ProductionRequest_ReturnsProductionOnly()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName: "PaymentService",
                environment: "Production",
                lookbackHours: 72,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root = json.RootElement;

        root
            .GetProperty("evidenceCount")
            .GetInt32()
            .Should()
            .Be(1);

        var evidence =
            root
                .GetProperty("evidence")
                .EnumerateArray()
                .Single();

        evidence
            .GetProperty("evidenceId")
            .GetString()
            .Should()
            .Be("DEP-EV-004");

        evidence
            .GetProperty("environment")
            .GetString()
            .Should()
            .Be("Production");
    }

    [Fact]
    public async Task
        GetRecentDeployments_CancelledRequest_ThrowsCancellation()
    {
        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        var action =
            async () =>
                await _tool.GetRecentDeploymentsAsync(
                    serviceName: "PaymentService",
                    environment: "Development",
                    lookbackHours: 24,
                    cancellationToken:
                        cancellationSource.Token);

        await action
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }
}