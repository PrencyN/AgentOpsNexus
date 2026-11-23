using AgentOpsNexus.AgentApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgentOpsNexus.AgentApi.IntegrationTests.Infrastructure;

public sealed class AgentApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public AgentApiWebApplicationFactory()
    {
        _databasePath =
            Path.Combine(
                Path.GetTempPath(),
                "AgentOpsNexus.IntegrationTests",
                $"{Guid.NewGuid():N}.db");

        var directory =
            Path.GetDirectoryName(
                _databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string DatabasePath =>
        _databasePath;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Agent:Provider"] =
                            "Mock",

                        ["Agent:ProjectEndpoint"] =
                            string.Empty,

                        ["Agent:ModelDeployment"] =
                            string.Empty,

                        ["Agent:TenantId"] =
                            string.Empty,

                        ["Agent:TimeoutSeconds"] =
                            "30",

                        ["ConnectionStrings:AgentOpsDatabase"] =
                            $"Data Source={_databasePath}"
                    });
            });

        builder.ConfigureServices(
            services =>
            {
                /*
                 * Program.cs registered the production
                 * DbContext before this test override runs.
                 *
                 * Remove every registration related to
                 * AgentOpsDbContext and replace it with an
                 * isolated SQLite database for this factory.
                 */
                services.RemoveAll<
                    DbContextOptions<
                        AgentOpsDbContext>>();

                services.RemoveAll<
                    AgentOpsDbContext>();

                services.AddDbContext<
                    AgentOpsDbContext>(
                        options =>
                        {
                            options.UseSqlite(
                                $"Data Source={_databasePath}");

                            options.EnableDetailedErrors();
                        });
            });
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri(
                        "http://localhost"),

                AllowAutoRedirect =
                    false
            });
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        DeleteIfPresent(
            _databasePath);

        DeleteIfPresent(
            $"{_databasePath}-shm");

        DeleteIfPresent(
            $"{_databasePath}-wal");
    }

    private static void DeleteIfPresent(
        string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            /*
             * Test cleanup must not hide the original
             * test result if SQLite still has a late file
             * handle during process shutdown.
             */
        }
        catch (UnauthorizedAccessException)
        {
            /*
             * Temporary files can be removed by the
             * operating system later.
             */
        }
    }
}