using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using QuranDashboard.Tests.TestSupport.Process;
using AccessAdminProgram = QuranDashboard.AccessAdmin.Program;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AccessMigrationTestFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var scratch = await ScratchDatabaseExecutionContext.ResolveAsync(
            QuranDashboard.Tests.TestRuntime.TestRuntimeTestPaths.ContractPath);
        ConnectionString = scratch.ConnectionString;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task<AccessMigrationDatabase> CreateDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                $"{nameof(AccessMigrationTestFixture)} hands out schemas only after the repository runner "
                + "has resolved its receipt-bound empty-scratch database.");
        }

        return new AccessMigrationDatabase(
            await PostgreSqlSchemaLease.CreateAsync(ConnectionString, nameof(AccessMigrationTestFixture)));
    }

    public async Task<AccessMigrationDatabase> CreateMigratedDatabaseAsync()
    {
        var database = await CreateDatabaseAsync();
        try
        {
            await using var db = database.CreateDbContext();
            await db.Database.MigrateAsync();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
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
        return await RunCoreAsync(connectionString, null, args);
    }

    internal static async Task<AccessAdminRun> RunAsync(
        string connectionString,
        IReadOnlyDictionary<string, string?> additionalEnvironmentVariables,
        params string[] args)
    {
        return await RunCoreAsync(connectionString, additionalEnvironmentVariables, args);
    }

    private static async Task<AccessAdminRun> RunCoreAsync(
        string connectionString,
        IReadOnlyDictionary<string, string?>? additionalEnvironmentVariables,
        params string[] args)
    {
        var environmentVariables = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [AccessAdminConnectionString.EnvironmentVariable] = connectionString,
            ["OwnerBootstrap__Emails__0"] = "owner-preflight@example.test",
        };
        if (additionalEnvironmentVariables is not null)
        {
            foreach (var environmentVariable in additionalEnvironmentVariables)
            {
                environmentVariables[environmentVariable.Key] = environmentVariable.Value;
            }
        }

        using var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: environmentVariables,
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

[CollectionDefinition(nameof(AccessScratchRehearsalCollection), DisableParallelization = true)]
public sealed class AccessScratchRehearsalCollection : ICollectionFixture<AccessMigrationTestFixture>;

[CollectionDefinition(nameof(PermissionCatalogueStartupScratchCollection), DisableParallelization = true)]
public sealed class PermissionCatalogueStartupScratchCollection : ICollectionFixture<AccessMigrationTestFixture>;
