using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessMigrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }

    public async Task<AccessMigrationDatabase> CreateDatabaseAsync()
    {
        var schemaName = $"access_migration_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE SCHEMA {schemaName};", connection);
        await command.ExecuteNonQueryAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            SearchPath = schemaName,
        }.ConnectionString;

        return new AccessMigrationDatabase(ConnectionString, connectionString, schemaName);
    }

    public async Task MigrateToAsync(QuranDashboardDbContext db, string migrationName)
    {
        var migrationId = db.Database.GetMigrations()
            .Single(migration => migration.EndsWith($"_{migrationName}", StringComparison.Ordinal));
        await db.Database.GetService<IMigrator>().MigrateAsync(migrationId);
    }

    public Task MigrateToHeadAsync(QuranDashboardDbContext db)
    {
        return db.Database.MigrateAsync();
    }
}

public sealed class AccessMigrationDatabase(
    string adminConnectionString,
    string connectionString,
    string schemaName) : IAsyncDisposable
{
    public string ConnectionString { get; } = connectionString;

    public QuranDashboardDbContext CreateDbContext()
    {
        return new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP SCHEMA {schemaName} CASCADE;", connection);
        await command.ExecuteNonQueryAsync();
    }
}

public sealed record AccessAdminRun(int ExitCode, string Output);

[CollectionDefinition(nameof(AccessMigrationCollection), DisableParallelization = true)]
public sealed class AccessMigrationCollection : ICollectionFixture<AccessMigrationTestFixture>;
