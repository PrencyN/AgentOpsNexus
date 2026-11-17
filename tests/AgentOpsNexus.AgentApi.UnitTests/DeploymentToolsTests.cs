using System.Diagnostics.Metrics;
using System.Text.Json;
using AgentOpsNexus.AgentApi.Observability;
using AgentOpsNexus.AgentApi.Services.Deployments;
using AgentOpsNexus.AgentApi.Tools;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentOpsNexus.AgentApi.UnitTests;

public sealed class DeploymentToolsTests
    : IDisposable
{
    private readonly ServiceProvider
        _serviceProvider;

    private readonly DeploymentTools
        _tool;

    public DeploymentToolsTests()
    {
        var services =
            new ServiceCollection();

        /*
         * Registers IMeterFactory for AgentOpsMetrics.
         *
         * No exporter is required for unit tests because
         * these tests verify tool behavior rather than
         * exported telemetry output.
         */
        services.AddMetrics();

        _serviceProvider =
            services.BuildServiceProvider();

        var meterFactory =
            _serviceProvider
                .GetRequiredService<
                    IMeterFactory>();

        var metrics =
            new AgentOpsMetrics(
                meterFactory);

        _tool =
            new DeploymentTools(
                new MockDeploymentEvidenceService(),
                metrics,
                NullLogger<DeploymentTools>.Instance);
    }

    [Fact]
    public async Task
        GetRecentDeployments_ValidPaymentServiceRequest_ReturnsEvidence()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    "PaymentService",
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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
            .GetProperty("lookbackHours")
            .GetInt32()
            .Should()
            .Be(24);

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
                            .GetProperty(
                                "evidenceId")
                            .GetString())
                .ToArray();

        evidenceIds.Should().Contain(
            "DEP-EV-001");

        evidenceIds.Should().Contain(
            "DEP-EV-002");
    }

    [Fact]
    public async Task
        GetRecentDeployments_ValidRequest_ReturnsExpectedDeploymentFields()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    "PaymentService",
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var firstEvidence =
            json.RootElement
                .GetProperty("evidence")
                .EnumerateArray()
                .First();

        firstEvidence
            .GetProperty("evidenceId")
            .GetString()
            .Should()
            .Be("DEP-EV-001");

        firstEvidence
            .GetProperty("serviceName")
            .GetString()
            .Should()
            .Be("PaymentService");

        firstEvidence
            .GetProperty("environment")
            .GetString()
            .Should()
            .Be("Development");

        firstEvidence
            .GetProperty("version")
            .GetString()
            .Should()
            .Be("2.4.1");

        firstEvidence
            .GetProperty("status")
            .GetString()
            .Should()
            .Be("Succeeded");

        firstEvidence
            .GetProperty("changeSummary")
            .GetString()
            .Should()
            .Contain(
                "timeout handling");

        firstEvidence
            .GetProperty("commitId")
            .GetString()
            .Should()
            .Be("a91f2d7");

        firstEvidence
            .GetProperty("pipelineRunId")
            .GetString()
            .Should()
            .Be("PIPE-1842");

        firstEvidence
            .GetProperty("source")
            .GetString()
            .Should()
            .Be("Mock deployment evidence");
    }

    [Fact]
    public async Task
        GetRecentDeployments_InputContainingWhitespace_IsNormalized()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    " PaymentService ",
                environment:
                    " Development ",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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
        GetRecentDeployments_ServiceAndEnvironmentMatching_IsCaseInsensitive()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    "paymentservice",
                environment:
                    "development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

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
                serviceName:
                    "PaymentService",
                environment:
                    "Unknown",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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

        root
            .GetProperty("error")
            .GetProperty("message")
            .GetString()
            .Should()
            .Contain(
                "Development, Test, Staging, " +
                "or Production");
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
                serviceName:
                    "PaymentService",
                environment:
                    "Development",
                lookbackHours:
                    invalidLookback,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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

        root
            .GetProperty("error")
            .GetProperty("message")
            .GetString()
            .Should()
            .Contain(
                "between 1 and 168");
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
                serviceName:
                    invalidServiceName,
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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

        root
            .GetProperty("error")
            .GetProperty("message")
            .GetString()
            .Should()
            .Contain(
                "between 2 and 100 characters");
    }

    [Fact]
    public async Task
        GetRecentDeployments_ServiceNameOverMaximumLength_ReturnsValidationError()
    {
        var serviceName =
            new string(
                'A',
                101);

        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    serviceName,
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        json.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        json.RootElement
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
                serviceName:
                    "UnknownService",
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

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
                serviceName:
                    "PaymentService",
                environment:
                    "Production",
                lookbackHours:
                    72,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

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

        evidence
            .GetProperty("version")
            .GetString()
            .Should()
            .Be("2.3.9");
    }

    [Fact]
    public async Task
        GetRecentDeployments_CheckoutServiceRequest_ReturnsCheckoutEvidence()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    "CheckoutService",
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        var root =
            json.RootElement;

        root
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeTrue();

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
            .Be("DEP-EV-003");

        evidence
            .GetProperty("serviceName")
            .GetString()
            .Should()
            .Be("CheckoutService");
    }

    [Fact]
    public async Task
        GetRecentDeployments_ResponseLabelsEvidenceAsMockData()
    {
        var result =
            await _tool.GetRecentDeploymentsAsync(
                serviceName:
                    "PaymentService",
                environment:
                    "Development",
                lookbackHours:
                    24,
                cancellationToken:
                    CancellationToken.None);

        using var json =
            JsonDocument.Parse(result);

        json.RootElement
            .GetProperty("limitations")
            .GetString()
            .Should()
            .Contain(
                "deterministic mock deployment records");

        json.RootElement
            .GetProperty("limitations")
            .GetString()
            .Should()
            .Contain(
                "does not come from a live deployment platform");
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
                await _tool
                    .GetRecentDeploymentsAsync(
                        serviceName:
                            "PaymentService",
                        environment:
                            "Development",
                        lookbackHours:
                            24,
                        cancellationToken:
                            cancellationSource.Token);

        await action
            .Should()
            .ThrowAsync<
                OperationCanceledException>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}