using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;
using AccessAdminProgram = QuranDashboard.AccessAdmin.Program;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessMigrationTestFixture : IAsyncLifetime
{
    private PostgreSqlDatabaseLease? _databaseLease;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _databaseLease = await PostgreSqlTestProcess.LeaseEmptyDatabaseAsync(nameof(AccessMigrationTestFixture));
        ConnectionString = _databaseLease.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_databaseLease is not null)
        {
            await _databaseLease.DisposeAsync();
            _databaseLease = null;
        }
    }

    public async Task<AccessMigrationDatabase> CreateDatabaseAsync()
    {
        var database = _databaseLease ?? throw new InvalidOperationException(
            $"{nameof(AccessMigrationTestFixture)} hands out migration schemas only after the collection has "
            + "initialized its empty database lease.");

        return new AccessMigrationDatabase(
            await PostgreSqlSchemaLease.CreateAsync(database, nameof(AccessMigrationTestFixture)));
    }

    public async Task MigrateToAsync(QuranDashboardDbContext db, string migrationName)
    {
        var migrationId = db.Database.GetMigrations()
            .Single(migration => migration.EndsWith($"_{migrationName}", StringComparison.Ordinal));
        await db.Database.GetService<IMigrator>().MigrateAsync(migrationId);
    }
}

public sealed class AccessMigrationDatabase : IAsyncDisposable
{
    private readonly PostgreSqlSchemaLease _schemaLease;

    internal AccessMigrationDatabase(PostgreSqlSchemaLease schemaLease)
    {
        _schemaLease = schemaLease;
    }

    public string ConnectionString => _schemaLease.ConnectionString;

    public QuranDashboardDbContext CreateDbContext()
    {
        return new QuranDashboardDbContext(
            new DbContextOptionsBuilder<QuranDashboardDbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    public ValueTask DisposeAsync()
    {
        return _schemaLease.DisposeAsync();
    }
}

internal static class AccessAdminInProcess
{
    internal static async Task<AccessAdminRun> RunAsync(
        string connectionString,
        params string[] args)
    {
        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [AccessAdminConnectionString.EnvironmentVariable] = connectionString,
                ["OwnerBootstrap__Emails__0"] = "owner-preflight@example.test",
            },
            captureConsole: true);

        var exitCode = await AccessAdminProgram.Main(args);

        return new AccessAdminRun(exitCode, processState.ConsoleOutput);
    }
}

internal static class AccessAdminConnectionString
{
    internal const string EnvironmentVariable = "ConnectionStrings__QuranDashboardDb";

    internal const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=absent;Username=absent;Password=absent;Timeout=2";
}

public sealed record AccessAdminRun(int ExitCode, string Output);

[CollectionDefinition(nameof(AccessProcessGlobalCollection), DisableParallelization = true)]
public sealed class AccessProcessGlobalCollection : ICollectionFixture<AccessMigrationTestFixture>;
