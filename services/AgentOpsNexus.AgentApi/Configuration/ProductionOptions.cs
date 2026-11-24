using System.ComponentModel.DataAnnotations;

namespace AgentOpsNexus.AgentApi.Configuration;

public sealed class ProductionOptions
{
    public const string SectionName =
        "Production";

    [Required]
    public string[] AllowedOrigins
    {
        get;
        init;
    } = [];

    public bool ApplyDatabaseMigrations
    {
        get;
        init;
    }

    public bool AllowSqlite
    {
        get;
        init;
    }
}