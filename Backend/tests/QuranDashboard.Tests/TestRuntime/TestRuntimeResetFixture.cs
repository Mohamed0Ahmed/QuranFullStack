using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.TestRuntime;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeResetFixture : IAsyncLifetime
{
    private ExclusivePostgreSqlLease? server;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(TestRuntimeResetFixture),
            "postgres:18-alpine");
        var serverConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Pooling = false,
        };
        var maintenanceConnection = new NpgsqlConnectionStringBuilder(serverConnection.ConnectionString)
        {
            Database = "postgres",
        };
        await using (var connection = new NpgsqlConnection(maintenanceConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard_test TEMPLATE template0");
        }

        var target = new NpgsqlConnectionStringBuilder(serverConnection.ConnectionString)
        {
            Database = "quran_dashboard_test",
        };
        ConnectionString = target.ConnectionString;
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new QuranDashboardDbContext(options))
        {
            await context.Database.MigrateAsync();
            await new PermissionCatalogueSynchronizer(context).SynchronizeAsync(CancellationToken.None);
        }

        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = await TestRuntimeCommand.ExecuteAsync(
            ["admin", "apply", "--login", serverConnection.Username!, "--run-id", "reset-fixture"],
            output,
            error,
            name => name == TestRuntimeCommand.DefaultConnectionStringEnvironmentVariable
                ? ConnectionString
                : null);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Capability setup failed: {error}{Environment.NewLine}{output}");
        }

        await using var markerConnection = new NpgsqlConnection(ConnectionString);
        await markerConnection.OpenAsync();
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var protectedState = await ProtectedStateFingerprint.ComputeAsync(markerConnection, contract);
        await ExecuteAsync(
            markerConnection,
            $"""
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.canonical_pipeline TO 'test-fixture';
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.canonical_input_provenance TO 'test-fixture';
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.canonical_quran_fingerprint TO 'test-fixture';
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.system_catalogue_fingerprint TO 'test-fixture';
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.protected_state_fingerprint TO '{protectedState.Fingerprint}';
            ALTER DATABASE quran_dashboard_test SET quran_dashboard.test_runtime.refreshed_at_utc TO '2026-09-03T00:00:00Z';
            """);
    }

    public async Task DisposeAsync()
    {
        if (server is not null)
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionString));
            await server.DisposeAsync();
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(TestRuntimeResetCollection), DisableParallelization = true)]
public sealed class TestRuntimeResetCollection : ICollectionFixture<TestRuntimeResetFixture>;
