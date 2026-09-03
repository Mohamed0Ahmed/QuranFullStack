using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeRefreshFixture : IAsyncLifetime
{
    private ExclusivePostgreSqlLease? server;

    public string ConnectionString { get; private set; } = string.Empty;

    public string Login { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(TestRuntimeRefreshFixture),
            "postgres:18-alpine");
        var serverConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Pooling = false,
            Database = "postgres",
        };
        Login = serverConnection.Username!;
        await using (var connection = new NpgsqlConnection(serverConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard_test TEMPLATE template0");
        }

        var developmentConnection = new NpgsqlConnectionStringBuilder(serverConnection.ConnectionString)
        {
            Database = "quran_dashboard",
        };
        await using (var connection = new NpgsqlConnection(developmentConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE TABLE development_probe (id integer PRIMARY KEY)");
            await ExecuteAsync(connection, "INSERT INTO development_probe VALUES (152)");
        }

        var targetConnection = new NpgsqlConnectionStringBuilder(serverConnection.ConnectionString)
        {
            Database = "quran_dashboard_test",
        };
        ConnectionString = targetConnection.ConnectionString;
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE old_capability_probe (id integer PRIMARY KEY)");
    }

    public async Task ResetTargetAsync()
    {
        var maintenance = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync();
        if (!await DatabaseExistsAsync(connection, "quran_dashboard_test"))
        {
            var staged = await FindDatabaseAsync(connection, "quran_dashboard_test_refresh_");
            if (staged is not null)
            {
                await ExecuteAsync(connection, $"ALTER DATABASE \"{staged}\" RENAME TO quran_dashboard_test");
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (server is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await server.DisposeAsync();
        }
    }

    internal static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private static async Task<bool> DatabaseExistsAsync(NpgsqlConnection connection, string database)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @database)",
            connection);
        command.Parameters.AddWithValue("database", database);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string?> FindDatabaseAsync(NpgsqlConnection connection, string prefix)
    {
        await using var command = new NpgsqlCommand(
            "SELECT datname FROM pg_database WHERE datname LIKE @prefix ORDER BY datname LIMIT 1",
            connection);
        command.Parameters.AddWithValue("prefix", prefix + "%");
        return await command.ExecuteScalarAsync() as string;
    }
}

[CollectionDefinition(nameof(TestRuntimeRefreshCollection), DisableParallelization = true)]
public sealed class TestRuntimeRefreshCollection : ICollectionFixture<TestRuntimeRefreshFixture>;
