using AgentOpsNexus.AgentApi.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AgentOpsNexus.AgentApi.Health;

public sealed class DatabaseReadinessCheck
    : IHealthCheck
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    public DatabaseReadinessCheck(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory =
            scopeFactory;
    }

    public async Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        try
        {
            await using var scope =
                _scopeFactory
                    .CreateAsyncScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<
                        AgentOpsDbContext>();

            var canConnect =
                await dbContext.Database
                    .CanConnectAsync(
                        cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult
                    .Unhealthy(
                        "The AgentOps database " +
                        "is not reachable.");
            }

            return HealthCheckResult
                .Healthy(
                    "The AgentOps database " +
                    "is reachable.");
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult
                .Unhealthy(
                    "The AgentOps database " +
                    "readiness check failed.",
                    exception);
        }
    }
}