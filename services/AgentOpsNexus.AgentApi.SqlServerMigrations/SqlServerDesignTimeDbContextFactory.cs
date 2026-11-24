using AgentOpsNexus.AgentApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgentOpsNexus.AgentApi.SqlServerMigrations;

public sealed class
    SqlServerDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<
        AgentOpsDbContext>
{
    public AgentOpsDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<
                AgentOpsDbContext>();

        /*
         * This connection string is used only by the
         * EF Core design-time model builder.
         *
         * Creating migrations does not connect to the
         * database. Never place production credentials
         * in this project.
         */
        var connectionString =
            string.Concat(
                "Server=",
                "(localdb)",
                "\\MSSQLLocalDB;",
                "Database=",
                "AgentOpsNexusMigrationDesign;",
                "Trusted_Connection=True;",
                "TrustServerCertificate=True;");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(
                    typeof(
                        SqlServerDesignTimeDbContextFactory)
                    .Assembly
                    .GetName()
                    .Name);
            });

        return new AgentOpsDbContext(
            optionsBuilder.Options);
    }
}