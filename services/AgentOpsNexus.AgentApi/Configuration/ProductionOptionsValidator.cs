using Microsoft.Extensions.Options;

namespace AgentOpsNexus.AgentApi.Configuration;

public sealed class ProductionOptionsValidator
    : IValidateOptions<ProductionOptions>
{
    private readonly IHostEnvironment
        _environment;

    private readonly IConfiguration
        _configuration;

    public ProductionOptionsValidator(
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _environment =
            environment;

        _configuration =
            configuration;
    }

    public ValidateOptionsResult Validate(
        string? name,
        ProductionOptions options)
    {
        if (!_environment.IsProduction())
        {
            return ValidateOptionsResult
                .Success;
        }

        var failures =
            new List<string>();

        ValidateAuthentication(
            failures);

        ValidateAgentProvider(
            failures);

        ValidateOrigins(
            options,
            failures);

        ValidatePersistence(
            options,
            failures);

        if (failures.Count == 0)
        {
            return ValidateOptionsResult
                .Success;
        }

        return ValidateOptionsResult.Fail(
            failures);
    }

    private void ValidateAuthentication(
        ICollection<string> failures)
    {
        var authenticationEnabled =
            _configuration.GetValue<bool>(
                "Authentication:Enabled");

        if (!authenticationEnabled)
        {
            failures.Add(
                "Production requires " +
                "Authentication:Enabled=true.");
        }
    }

    private void ValidateAgentProvider(
        ICollection<string> failures)
    {
        var provider =
            _configuration[
                "Agent:Provider"];

        if (!string.Equals(
                provider,
                "Foundry",
                StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                "Production requires " +
                "Agent:Provider=Foundry.");
        }
    }

    private static void ValidateOrigins(
        ProductionOptions options,
        ICollection<string> failures)
    {
        if (
            options.AllowedOrigins is null ||
            options.AllowedOrigins.Length == 0)
        {
            failures.Add(
                "Production requires at least one " +
                "Production:AllowedOrigins value.");

            return;
        }

        foreach (
            var configuredOrigin in
                options.AllowedOrigins)
        {
            var origin =
                configuredOrigin?.Trim();

            if (string.IsNullOrWhiteSpace(
                    origin))
            {
                failures.Add(
                    "Production allowed origins " +
                    "cannot contain an empty value.");

                continue;
            }

            if (!Uri.TryCreate(
                    origin,
                    UriKind.Absolute,
                    out var originUri))
            {
                failures.Add(
                    $"Production origin '{origin}' " +
                    "is not an absolute URI.");

                continue;
            }

            var validScheme =
                string.Equals(
                    originUri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    originUri.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase);

            if (!validScheme)
            {
                failures.Add(
                    $"Production origin '{origin}' " +
                    "must use HTTP or HTTPS.");
            }

            if (
                !string.Equals(
                    originUri.AbsolutePath,
                    "/",
                    StringComparison.Ordinal)
                ||
                !string.IsNullOrEmpty(
                    originUri.Query)
                ||
                !string.IsNullOrEmpty(
                    originUri.Fragment)
                ||
                !string.IsNullOrEmpty(
                    originUri.UserInfo))
            {
                failures.Add(
                    $"Production origin '{origin}' " +
                    "must contain only scheme, host, " +
                    "and optional port.");
            }

            if (
                _environmentIsProductionHttp(
                    originUri))
            {
                failures.Add(
                    $"Production origin '{origin}' " +
                    "must use HTTPS unless it is a " +
                    "loopback development origin.");
            }
        }
    }

    private static bool
        _environmentIsProductionHttp(
            Uri originUri)
    {
        if (!string.Equals(
                originUri.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return
            !originUri.IsLoopback;
    }

    private void ValidatePersistence(
        ProductionOptions options,
        ICollection<string> failures)
    {
        var connectionString =
            _configuration.GetConnectionString(
                "AgentOpsDatabase")
            ??
            string.Empty;

        var usesSqlite =
            connectionString.Contains(
                "Data Source=",
                StringComparison.OrdinalIgnoreCase)
            ||
            connectionString.Contains(
                "Filename=",
                StringComparison.OrdinalIgnoreCase);

        if (
            usesSqlite &&
            !options.AllowSqlite)
        {
            failures.Add(
                "Production is configured with " +
                "SQLite, but " +
                "Production:AllowSqlite is false. " +
                "Use managed production persistence " +
                "or explicitly accept the " +
                "single-instance SQLite constraint.");
        }
    }
}