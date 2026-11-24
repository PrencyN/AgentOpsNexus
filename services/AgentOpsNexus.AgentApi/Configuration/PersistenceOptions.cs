using System.ComponentModel.DataAnnotations;

namespace AgentOpsNexus.AgentApi.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName =
        "Persistence";

    public const string SqliteProvider =
        "Sqlite";

    public const string SqlServerProvider =
        "SqlServer";

    public const string DefaultConnectionStringName =
        "AgentOpsDatabase";

    [Required]
    public string Provider
    {
        get;
        init;
    } =
        SqliteProvider;

    [Required]
    public string ConnectionStringName
    {
        get;
        init;
    } =
        DefaultConnectionStringName;

    public bool IsSqlite =>
        string.Equals(
            Provider,
            SqliteProvider,
            StringComparison.OrdinalIgnoreCase);

    public bool IsSqlServer =>
        string.Equals(
            Provider,
            SqlServerProvider,
            StringComparison.OrdinalIgnoreCase);
}