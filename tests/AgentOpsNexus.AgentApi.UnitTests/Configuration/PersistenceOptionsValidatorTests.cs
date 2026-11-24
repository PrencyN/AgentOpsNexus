using AgentOpsNexus.AgentApi.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.UnitTests.Configuration;

public sealed class
    PersistenceOptionsValidatorTests
{
    [Fact]
    public void
        Sqlite_WithConnectionString_IsAccepted()
    {
        var validator =
            CreateValidator(
                connectionString:
                    "Data Source=App_Data/agentops.db");

        var options =
            CreateOptions(
                PersistenceOptions
                    .SqliteProvider);

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
        SqlServer_WithConnectionString_IsAccepted()
    {
        var validator =
            CreateValidator(
                connectionString:
                    CreateSqlServerConnectionString());

        var options =
            CreateOptions(
                PersistenceOptions
                    .SqlServerProvider);

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
        UnsupportedProvider_IsRejected()
    {
        var validator =
            CreateValidator();

        var options =
            CreateOptions(
                provider:
                    "PostgreSql");

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
            .ContainSingle();

        GetFailures(result)
            .Single()
            .Should()
            .Contain(
                "Persistence:Provider");

        GetFailures(result)
            .Single()
            .Should()
            .Contain(
                PersistenceOptions
                    .SqliteProvider);

        GetFailures(result)
            .Single()
            .Should()
            .Contain(
                PersistenceOptions
                    .SqlServerProvider);
    }

    [Fact]
    public void
        EmptyConnectionStringName_IsRejected()
    {
        var validator =
            CreateValidator();

        var options =
            new PersistenceOptions
            {
                Provider =
                    PersistenceOptions
                        .SqliteProvider,

                ConnectionStringName =
                    string.Empty
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
                        "ConnectionStringName",
                        StringComparison.Ordinal));
    }

    [Fact]
    public void
        MissingConfiguredConnectionString_IsRejected()
    {
        var validator =
            CreateValidator(
                includeConnectionString:
                    false);

        var options =
            CreateOptions(
                PersistenceOptions
                    .SqlServerProvider);

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
                        PersistenceOptions
                            .DefaultConnectionStringName,
                        StringComparison.Ordinal)
                    &&
                    failure.Contains(
                        "required",
                        StringComparison
                            .OrdinalIgnoreCase));
    }

    private static
        PersistenceOptionsValidator
        CreateValidator(
            string connectionString =
                "Data Source=App_Data/agentops.db",
            bool includeConnectionString =
                true)
    {
        var configurationValues =
            new Dictionary<
                string,
                string?>();

        if (includeConnectionString)
        {
            configurationValues[
                "ConnectionStrings:" +
                PersistenceOptions
                    .DefaultConnectionStringName] =
                connectionString;
        }

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    configurationValues)
                .Build();

        return new
            PersistenceOptionsValidator(
                configuration);
    }

    private static PersistenceOptions
        CreateOptions(
            string provider)
    {
        return new PersistenceOptions
        {
            Provider =
                provider,

            ConnectionStringName =
                PersistenceOptions
                    .DefaultConnectionStringName
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

    private static string
        CreateSqlServerConnectionString()
    {
        return string.Concat(
            "Server=tcp:",
            "agentops.database.windows.net,1433;",
            "Database=AgentOps;",
            "Encrypt=True;",
            "TrustServerCertificate=False;");
    }
}
