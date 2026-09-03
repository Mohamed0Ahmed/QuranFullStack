using Microsoft.EntityFrameworkCore;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.TestRuntime;

public sealed class TestRuntimeAdministrationFixture : IAsyncLifetime
{
    private ExclusivePostgreSqlLease? server;

    internal string CredentialSentinel { get; } = $"test-runtime-{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = string.Empty;

    public string Login { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        server = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(TestRuntimeAdministrationFixture),
            "postgres:18-alpine",
            builder => builder.WithPassword(CredentialSentinel));
        var containerConnection = new NpgsqlConnectionStringBuilder(server.ConnectionString)
        {
            Pooling = false,
        };
        Login = containerConnection.Username!;

        var maintenanceConnection = new NpgsqlConnectionStringBuilder(containerConnection.ConnectionString)
        {
            Database = "postgres",
        };
        await using (var connection = new NpgsqlConnection(maintenanceConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard TEMPLATE template0");
            await ExecuteAsync(connection, "CREATE DATABASE quran_dashboard_test TEMPLATE template0");
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
        };
        ConnectionString = targetConnection.ConnectionString;
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new QuranDashboardDbContext(options);
        await context.Database.MigrateAsync();
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

[CollectionDefinition(nameof(TestRuntimeAdministrationCollection), DisableParallelization = true)]
public sealed class TestRuntimeAdministrationCollection : ICollectionFixture<TestRuntimeAdministrationFixture>;
