using AgentOpsNexus.AgentApi.Data;
using AgentOpsNexus.AgentApi.Configuration;
using Microsoft.AspNetCore.Authentication;
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
            Directory.CreateDirectory(
                directory);
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
                configurationBuilder
                    .AddInMemoryCollection(
                        new Dictionary<
                            string,
                            string?>
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

                            ["Persistence:Provider"] =
                                PersistenceOptions.SqliteProvider,

                            ["Persistence:ConnectionStringName"] =
                                PersistenceOptions
                                    .DefaultConnectionStringName,

                            /*
                             * Program.cs registers its
                             * local authentication scheme
                             * when this is false.
                             *
                             * The service registration
                             * below replaces the defaults
                             * with the header-driven test
                             * scheme.
                             */
                            ["Authentication:Enabled"] =
                                "false",

                            ["ConnectionStrings:" +
                             "AgentOpsDatabase"] =
                                $"Data Source=" +
                                $"{_databasePath}"
                        });
            });

        builder.ConfigureServices(
            services =>
            {
                /*
                 * Program.cs registers the production
                 * DbContext before this test override.
                 *
                 * Replace it with an isolated SQLite
                 * database for this factory instance.
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
                                $"Data Source=" +
                                $"{_databasePath}");

                            options
                                .EnableDetailedErrors();
                        });

                /*
                 * Make the header-driven integration-test
                 * scheme the default authentication,
                 * challenge, and forbid scheme.
                 *
                 * Existing tests receive the default
                 * investigator identity. Authorization
                 * tests can change the identity through
                 * X-Test-* request headers.
                 */
                services
                    .AddAuthentication(
                        options =>
                        {
                            options
                                .DefaultAuthenticateScheme =
                                TestIdentity
                                    .AuthenticationScheme;

                            options
                                .DefaultChallengeScheme =
                                TestIdentity
                                    .AuthenticationScheme;

                            options
                                .DefaultForbidScheme =
                                TestIdentity
                                    .AuthenticationScheme;
                        })
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthenticationHandler>(
                            TestIdentity
                                .AuthenticationScheme,
                            _ =>
                            {
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
                        string.Concat(
                            "http",
                            "://",
                            "localhost")),

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
             * Cleanup must not hide the test result if
             * SQLite retains a late file handle while the
             * application host is shutting down.
             */
        }
        catch (UnauthorizedAccessException)
        {
            /*
             * The operating system can remove temporary
             * test files later.
             */
        }
    }
}