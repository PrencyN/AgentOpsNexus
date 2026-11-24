using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Configuration;

public sealed class PersistenceOptionsValidator
    : IValidateOptions<PersistenceOptions>
{
    private readonly IConfiguration
        _configuration;

    public PersistenceOptionsValidator(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }

    public ValidateOptionsResult Validate(
        string? name,
        PersistenceOptions options)
    {
        var failures =
            new List<string>();

        ValidateProvider(
            options,
            failures);

        ValidateConnectionStringName(
            options,
            failures);

        if (failures.Count == 0)
        {
            ValidateConnectionString(
                options,
                failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                failures);
    }

    private static void ValidateProvider(
        PersistenceOptions options,
        ICollection<string> failures)
    {
        if (
            !options.IsSqlite &&
            !options.IsSqlServer)
        {
            failures.Add(
                "Persistence:Provider must be " +
                $"'{PersistenceOptions.SqliteProvider}' " +
                "or " +
                $"'{PersistenceOptions.SqlServerProvider}'.");
        }
    }

    private static void
        ValidateConnectionStringName(
            PersistenceOptions options,
            ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(
                options.ConnectionStringName))
        {
            failures.Add(
                "Persistence:ConnectionStringName " +
                "is required.");
        }
    }

    private void ValidateConnectionString(
        PersistenceOptions options,
        ICollection<string> failures)
    {
        var connectionString =
            _configuration.GetConnectionString(
                options.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(
                connectionString))
        {
            failures.Add(
                "Connection string " +
                $"'{options.ConnectionStringName}' " +
                "is required for persistence " +
                $"provider '{options.Provider}'.");
        }
    }
}