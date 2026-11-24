using AgentOpsNexus.AgentApi.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.UnitTests.Configuration;

public sealed class
    ProductionOptionsValidatorTests
{
    [Fact]
    public void
        NonProduction_AllowsDevelopmentConfiguration()
    {
        var validator =
            CreateValidator(
                environmentName:
                    Environments.Development,
                authenticationEnabled:
                    false,
                agentProvider:
                    "Mock",
                persistenceProvider:
                    PersistenceOptions
                        .SqliteProvider);

        var options =
            new ProductionOptions
            {
                AllowedOrigins =
                    [],
                ApplyDatabaseMigrations =
                    true
            };

        var result =
            validator.Validate(
                name:
                    null,
                options);

        result.Succeeded
            .Should()
            .BeTrue();

        result.Failed
            .Should()
            .BeFalse();
    }

    [Fact]
    public void
        Production_AuthenticationDisabled_IsRejected()
    {
        var validator =
            CreateValidator(
                authenticationEnabled:
                    false);

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions());

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "Authentication:Enabled=true",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_MockAgent_IsRejected()
    {
        var validator =
            CreateValidator(
                agentProvider:
                    "Mock");

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions());

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "Agent:Provider=Foundry",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_SqliteProvider_IsRejected()
    {
        var validator =
            CreateValidator(
                persistenceProvider:
                    PersistenceOptions
                        .SqliteProvider);

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions());

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "Persistence:Provider=SqlServer",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_EmptyOrigins_IsRejected()
    {
        var validator =
            CreateValidator();

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions(
                    allowedOrigins:
                        []));

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "at least one",
                        StringComparison
                            .OrdinalIgnoreCase)
                    &&
                    failure.Contains(
                        "AllowedOrigins",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_InvalidOrigin_IsRejected()
    {
        var validator =
            CreateValidator();

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions(
                    allowedOrigins:
                        [
                            "not-an-origin"
                        ]));

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "not an absolute URI",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_OriginWithPath_IsRejected()
    {
        var validator =
            CreateValidator();

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions(
                    allowedOrigins:
                        [
                            CreateHttpsUri(
                                "agentops.example.com",
                                "/application")
                        ]));

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "only scheme, host",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_NonHttpsRemoteOrigin_IsRejected()
    {
        var validator =
            CreateValidator();

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions(
                    allowedOrigins:
                        [
                            CreateHttpUri(
                                "agentops.example.com")
                        ]));

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "must use HTTPS",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_AutomaticMigrations_IsRejected()
    {
        var validator =
            CreateValidator();

        var options =
            new ProductionOptions
            {
                AllowedOrigins =
                    [
                        CreateHttpsUri(
                            "agentops.example.com")
                    ],
                ApplyDatabaseMigrations =
                    true
            };

        var result =
            validator.Validate(
                name:
                    null,
                options);

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "ApplyDatabaseMigrations=false",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        Production_ValidConfiguration_IsAccepted()
    {
        var validator =
            CreateValidator();

        var result =
            validator.Validate(
                name:
                    null,
                CreateValidOptions());

        result.Succeeded
            .Should()
            .BeTrue();

        result.Failed
            .Should()
            .BeFalse();
    }

    [Fact]
    public void
        Production_MultipleFailures_AreReturnedTogether()
    {
        var validator =
            CreateValidator(
                authenticationEnabled:
                    false,
                agentProvider:
                    "Mock",
                persistenceProvider:
                    PersistenceOptions
                        .SqliteProvider);

        var options =
            new ProductionOptions
            {
                AllowedOrigins =
                    [],
                ApplyDatabaseMigrations =
                    true
            };

        var result =
            validator.Validate(
                name:
                    null,
                options);

        result.Failed
            .Should()
            .BeTrue();

        GetFailures(result)
            .Should()
            .HaveCount(5);

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "Authentication",
                        StringComparison.Ordinal));

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "Foundry",
                        StringComparison.Ordinal));

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "SqlServer",
                        StringComparison.Ordinal));

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "AllowedOrigins",
                        StringComparison.Ordinal));

        GetFailures(result)
            .Should()
            .Contain(
                failure =>
                    failure.Contains(
                        "ApplyDatabaseMigrations",
                        StringComparison.Ordinal));
    }

    private static
        ProductionOptionsValidator
        CreateValidator(
            string? environmentName =
                null,
            bool authenticationEnabled =
                true,
            string agentProvider =
                "Foundry",
            string persistenceProvider =
                PersistenceOptions
                    .SqlServerProvider)
    {
        var configurationValues =
            new Dictionary<
                string,
                string?>
            {
                ["Authentication:Enabled"] =
                    authenticationEnabled
                        .ToString(),

                ["Agent:Provider"] =
                    agentProvider,

                ["Persistence:Provider"] =
                    persistenceProvider
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        var environment =
            new TestHostEnvironment
            {
                EnvironmentName =
                    environmentName ??
                    Environments.Production
            };

        return new
            ProductionOptionsValidator(
                environment,
                configuration);
    }

    private static ProductionOptions
        CreateValidOptions(
            string[]? allowedOrigins =
                null)
    {
        return new ProductionOptions
        {
            AllowedOrigins =
                allowedOrigins ??
                [
                    CreateHttpsUri(
                        "agentops.example.com")
                ],

            ApplyDatabaseMigrations =
                false
        };
    }

    private static IReadOnlyCollection<string>
        GetFailures(
            ValidateOptionsResult result)
    {
        return (
            result.Failures ??
            [])
            .ToArray();
    }

    private static string CreateHttpsUri(
        string host,
        string path = "")
    {
        return string.Concat(
            "https",
            "://",
            host,
            path);
    }

    private static string CreateHttpUri(
        string host)
    {
        return string.Concat(
            "http",
            "://",
            host);
    }

    private sealed class TestHostEnvironment
        : IHostEnvironment
    {
        public string EnvironmentName
        {
            get;
            set;
        } =
            Environments.Production;

        public string ApplicationName
        {
            get;
            set;
        } =
            "AgentOpsNexus.Tests";

        public string ContentRootPath
        {
            get;
            set;
        } =
            AppContext.BaseDirectory;

        public IFileProvider
            ContentRootFileProvider
        {
            get;
            set;
        } =
            new NullFileProvider();
    }
}
