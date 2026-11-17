using AgentOpsNexus.AgentApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgentOpsNexus.AgentApi.Data;

public sealed class AgentOpsDbContext : DbContext
{
    public AgentOpsDbContext(
        DbContextOptions<AgentOpsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AgentRequestRecord> AgentRequests =>
        Set<AgentRequestRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AgentOpsDbContext).Assembly);
    }
}