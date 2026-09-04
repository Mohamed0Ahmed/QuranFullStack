using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.Testing;

public sealed class DatabaseActivityProfileFixture : IAsyncLifetime
{
    private ExclusivePostgreSqlLease? server;
    private string login = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;

    public string ScratchConnectionString { get; private set; } = string.Empty;

    public string FullRehearsalConnectionString { get; private set; } = string.Empty;

    public string UnmarkedScratchConnectionString { get; private set; } = string.Empty;

    public string Login => login;

    internal TestSqlCommandCapture CommandCapture { get; } = new();

    public async Task InitializeAsync()
    {
        server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(DatabaseActivityProfileFixture),
            "postgres:18-alpine");
        var containerConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Pooling = false,
        };
        login = containerConnection.Username!;

        var maintenanceConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "postgres",
        };
        await using (var connection = new NpgsqlConnection(maintenanceConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard_test TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_test_scratch_profile_run TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_rehearsal_profile TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_test_scratch_unmarked TEMPLATE template0");
            await ApplyRehearsalMarkersAsync(connection, "quran_test_scratch_profile_run", full: false);
            await ApplyRehearsalMarkersAsync(connection, "quran_rehearsal_profile", full: true);
        }

        var developmentConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_dashboard",
        };
        await using (var connection = new NpgsqlConnection(developmentConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE SCHEMA authored");
            await ExecuteAsync(connection, "CREATE TABLE authored.developer_owned_probe (id integer PRIMARY KEY)");
        }

        var targetConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_dashboard_test",
            Pooling = true,
            MaxPoolSize = 1,
        };
        ConnectionString = targetConnection.ConnectionString;
        await MigrateAsync(ConnectionString);

        var scratchConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_test_scratch_profile_run",
            Pooling = true,
            MaxPoolSize = 1,
        };
        ScratchConnectionString = scratchConnection.ConnectionString;
        await MigrateAsync(ScratchConnectionString);

        var fullRehearsalConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_rehearsal_profile",
            Pooling = true,
            MaxPoolSize = 1,
        };
        FullRehearsalConnectionString = fullRehearsalConnection.ConnectionString;
        await MigrateAsync(FullRehearsalConnectionString);

        var unmarkedScratchConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "quran_test_scratch_unmarked",
            Pooling = true,
            MaxPoolSize = 1,
        };
        UnmarkedScratchConnectionString = unmarkedScratchConnection.ConnectionString;

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await QuranDashboard.TestRuntime.TestRuntimeCommand.ExecuteAsync(
            [
                "admin",
                "apply",
                "--login",
                login,
                "--run-id",
                PostgreSqlResourceLabels.RunId,
            ],
            output,
            error,
            name => name == QuranDashboard.TestRuntime.TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? ConnectionString
                : null);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Test Database capability role setup failed: {error}{output}");
        }
    }

    public async Task DisposeAsync()
    {
        if (server is not null)
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionString));
            NpgsqlConnection.ClearPool(new NpgsqlConnection(ScratchConnectionString));
            NpgsqlConnection.ClearPool(new NpgsqlConnection(FullRehearsalConnectionString));
            NpgsqlConnection.ClearPool(new NpgsqlConnection(UnmarkedScratchConnectionString));
            await server.DisposeAsync();
        }
    }

    public WebApplicationFactory<HealthController> BuildFactory(
        string? profile,
        params string[] backgroundActivities)
    {
        return new WebApplicationFactory<HealthController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:QuranDashboardDb", ConnectionString);
                if (profile is not null)
                {
                    builder.UseSetting("Testing:DatabaseActivity:Profile", profile);
                }
                for (var index = 0; index < backgroundActivities.Length; index++)
                {
                    builder.UseSetting(
                        $"Testing:DatabaseActivity:EnabledBackgroundActivities:{index}",
                        backgroundActivities[index]);
                }
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        ["OwnerBootstrap:Emails:0"] = "owner@example.test",
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    };
                    configuration.AddInMemoryCollection(settings);
                });
                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<QuranDashboardDbContext>(options =>
                        options.AddInterceptors(CommandCapture));
                    TestJwtTokens.ConfigureOfflineValidation(services);
                });
            });
    }

    public WebApplicationFactory<HealthController> BuildValidatedScratchFactory() =>
        BuildValidatedRehearsalFactory(
            ScratchConnectionString,
            "scratch-empty",
            "quran_test_scratch_profile_run",
            "migration");

    public WebApplicationFactory<HealthController> BuildValidatedFullRehearsalFactory() =>
        BuildValidatedRehearsalFactory(
            FullRehearsalConnectionString,
            "rehearsal-full",
            "quran_rehearsal_profile",
            "recovery");

    public WebApplicationFactory<HealthController> BuildSpoofedUnmarkedScratchFactory()
    {
        var connection = new NpgsqlConnectionStringBuilder(UnmarkedScratchConnectionString)
        {
            Options = "-c quran_dashboard.test_runtime.rehearsal_enabled=true "
                + "-c quran_dashboard.test_runtime.rehearsal_subtype=migration",
        };
        return BuildValidatedRehearsalFactory(
            connection.ConnectionString,
            "scratch-empty",
            "quran_test_scratch_unmarked",
            "migration");
    }

    private WebApplicationFactory<HealthController> BuildValidatedRehearsalFactory(
        string connectionString,
        string kind,
        string database,
        string subtype)
    {
        return new WebApplicationFactory<HealthController>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:QuranDashboardDb", connectionString);
                builder.UseSetting("Testing:DatabaseActivity:Profile", "DestructiveRehearsal");
                builder.UseSetting(
                    "Testing:DatabaseActivity:ValidatedRehearsalTarget:Kind",
                    kind);
                builder.UseSetting(
                    "Testing:DatabaseActivity:ValidatedRehearsalTarget:Database",
                    database);
                builder.UseSetting(
                    "Testing:DatabaseActivity:ValidatedRehearsalTarget:Subtype",
                    subtype);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = connectionString,
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        ["OwnerBootstrap:Emails:0"] = "owner@example.test",
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<QuranDashboardDbContext>(options =>
                        options.AddInterceptors(CommandCapture));
                    TestJwtTokens.ConfigureOfflineValidation(services);
                });
            });
    }

    public async Task<long> CountPermissionsAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT count(*) FROM public.permissions", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task MigrateAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        await context.Database.MigrateAsync();
    }

    private static async Task ApplyRehearsalMarkersAsync(
        NpgsqlConnection connection,
        string database,
        bool full)
    {
        var subtype = full ? "recovery" : "migration";
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.rehearsal_enabled TO 'true'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.rehearsal_subtype TO '{subtype}'");
        if (!full)
        {
            return;
        }

        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.canonical_pipeline TO 'test-pipeline'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.canonical_input_provenance TO 'test-provenance'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.canonical_quran_fingerprint TO 'test-quran-fingerprint'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.system_catalogue_fingerprint TO 'test-catalogue-fingerprint'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.migration_head TO 'test-migration-head'");
        await ExecuteAsync(
            connection,
            $"ALTER DATABASE {database} SET quran_dashboard.test_runtime.refreshed_at_utc TO '2026-09-03T00:00:00Z'");
    }
}

[CollectionDefinition(nameof(DatabaseActivityProfileCollection), DisableParallelization = true)]
public sealed class DatabaseActivityProfileCollection : ICollectionFixture<DatabaseActivityProfileFixture>;
