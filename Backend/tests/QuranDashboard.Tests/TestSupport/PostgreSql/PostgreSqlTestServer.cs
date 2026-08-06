using System.Collections.Concurrent;
using System.Globalization;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal sealed class PostgreSqlTestServer : IAsyncDisposable
{
    internal const string Image = "postgres:16-alpine";
    internal const string DatabaseParallelismVariable = "QURAN_DASHBOARD_TEST_DB_PARALLELISM";
    internal const int MaximumDatabaseParallelism = 4;

    private static readonly TimeSpan MaintenanceCommandTimeout = TimeSpan.FromMinutes(2);

    private readonly PostgreSqlContainer container;
    private readonly CrossProcessPostgreSqlLock crossProcessLock;
    private readonly SemaphoreSlim databaseSlots;
    private readonly SemaphoreSlim definitionGate = new(1, 1);
    private readonly ConcurrentDictionary<string, byte> createdDatabases = new(StringComparer.Ordinal);
    private readonly Lazy<Task<string>> migratedTemplateLoader;

    private int disposed;

    private PostgreSqlTestServer(
        PostgreSqlContainer container,
        CrossProcessPostgreSqlLock crossProcessLock,
        int databaseParallelism)
    {
        this.container = container;
        this.crossProcessLock = crossProcessLock;
        ConfiguredDatabaseSlots = databaseParallelism;
        databaseSlots = new SemaphoreSlim(databaseParallelism, databaseParallelism);
        migratedTemplateLoader = new Lazy<Task<string>>(
            BuildMigratedTemplateAsync,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal Guid ServerInstanceId { get; } = Guid.NewGuid();

    internal int ConfiguredDatabaseSlots { get; }

    internal int AvailableDatabaseSlots => databaseSlots.CurrentCount;

    internal static int ResolveDatabaseParallelism()
    {
        return ResolveDatabaseParallelism(Environment.GetEnvironmentVariable(DatabaseParallelismVariable));
    }

    internal static int ResolveDatabaseParallelism(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return MaximumDatabaseParallelism;
        }

        if (!int.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, out var parallelism)
            || parallelism < 1
            || parallelism > MaximumDatabaseParallelism)
        {
            throw new InvalidOperationException(
                $"{DatabaseParallelismVariable} accepts only integers 1-{MaximumDatabaseParallelism}, "
                + $"found '{configured}'. Raising the cap requires a measured lifecycle review.");
        }

        return parallelism;
    }

    internal static async Task<PostgreSqlTestServer> StartAsync(CancellationToken cancellationToken)
    {
        var databaseParallelism = ResolveDatabaseParallelism();
        var crossProcessLock = await CrossProcessPostgreSqlLock.AcquireProjectLockAsync(
            $"pid {Environment.ProcessId} shared {Image}",
            cancellationToken);

        try
        {
            var builder = new PostgreSqlBuilder().WithImage(Image);
            foreach (var label in PostgreSqlResourceLabels.ForPostgreSql())
            {
                builder = builder.WithLabel(label.Key, label.Value);
            }

            var container = builder.Build();
            try
            {
                await container.StartAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                await container.DisposeAsync();
                throw new InvalidOperationException(
                    $"The shared {Image} test container could not start. Backend database tests require a "
                    + "working Docker daemon on this host.",
                    exception);
            }

            return new PostgreSqlTestServer(container, crossProcessLock, databaseParallelism);
        }
        catch
        {
            crossProcessLock.Dispose();
            throw;
        }
    }

    internal Task<PostgreSqlDatabaseLease> LeaseMigratedDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        return LeaseDatabaseAsync(owner, migrated: true, cancellationToken);
    }

    internal Task<PostgreSqlDatabaseLease> LeaseEmptyDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        return LeaseDatabaseAsync(owner, migrated: false, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        foreach (var databaseName in createdDatabases.Keys)
        {
            await ReportFailureAsync(
                $"drop database {databaseName}",
                () => DropDatabaseAsync(databaseName));
        }

        await ReportFailureAsync("dispose container", async () => await container.DisposeAsync());
        crossProcessLock.Dispose();
    }

    private async Task<PostgreSqlDatabaseLease> LeaseDatabaseAsync(
        string owner,
        bool migrated,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);

        await databaseSlots.WaitAsync(cancellationToken);
        var slotHeld = true;
        string? databaseName = null;
        try
        {
            var template = migrated
                ? await migratedTemplateLoader.Value.WaitAsync(cancellationToken)
                : "template0";

            databaseName = PostgreSqlDatabaseName.CreateForOwner(owner);
            createdDatabases[databaseName] = 0;
            await CreateDatabaseAsync(databaseName, template, cancellationToken);

            var lease = PostgreSqlDatabaseLease.Owned(
                owner,
                databaseName,
                BuildConnectionString(databaseName, pooling: true),
                ServerInstanceId,
                ReleaseAsync);
            slotHeld = false;
            return lease;
        }
        catch
        {
            if (databaseName is not null)
            {
                createdDatabases.TryRemove(databaseName, out _);
            }

            throw;
        }
        finally
        {
            if (slotHeld)
            {
                databaseSlots.Release();
            }
        }
    }

    private async ValueTask ReleaseAsync(PostgreSqlDatabaseLease lease)
    {
        try
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(lease.ConnectionString));
            await ReportFailureAsync(
                $"drop database {lease.DatabaseName}",
                () => DropDatabaseAsync(lease.DatabaseName));
        }
        finally
        {
            databaseSlots.Release();
        }
    }

    private async Task<string> BuildMigratedTemplateAsync()
    {
        var templateName = PostgreSqlDatabaseName.CreateTemplate();
        createdDatabases[templateName] = 0;
        await CreateDatabaseAsync(templateName, "template0", CancellationToken.None);

        var connectionString = BuildConnectionString(templateName, pooling: false);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = connectionString
            })
            .Build();

        await using (var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider())
        {
            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
            await dbContext.Database.MigrateAsync();

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                throw new InvalidOperationException(
                    "The migrated template database is not at migration head; pending: "
                    + string.Join(", ", pendingMigrations));
            }
        }

        NpgsqlConnection.ClearPool(new NpgsqlConnection(connectionString));
        await ExecuteMaintenanceAsync(
            $"ALTER DATABASE {PostgreSqlDatabaseName.Quote(templateName)} WITH ALLOW_CONNECTIONS false",
            CancellationToken.None);
        await ExecuteMaintenanceAsync(
            $"ALTER DATABASE {PostgreSqlDatabaseName.Quote(templateName)} WITH IS_TEMPLATE true",
            CancellationToken.None);

        return templateName;
    }

    private Task CreateDatabaseAsync(string databaseName, string template, CancellationToken cancellationToken)
    {
        return ExecuteMaintenanceAsync(
            $"CREATE DATABASE {PostgreSqlDatabaseName.Quote(databaseName)} "
            + $"TEMPLATE {PostgreSqlDatabaseName.Quote(template)}",
            cancellationToken);
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        var quoted = PostgreSqlDatabaseName.Quote(databaseName);
        await ExecuteMaintenanceAsync(
            $"ALTER DATABASE {quoted} WITH IS_TEMPLATE false ALLOW_CONNECTIONS true",
            CancellationToken.None,
            ignoreMissingDatabase: true);
        await ExecuteMaintenanceAsync(
            $"DROP DATABASE IF EXISTS {quoted} WITH (FORCE)",
            CancellationToken.None);
        createdDatabases.TryRemove(databaseName, out _);
    }

    private async Task ExecuteMaintenanceAsync(
        string sql,
        CancellationToken cancellationToken,
        bool ignoreMissingDatabase = false)
    {
        await definitionGate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new NpgsqlConnection(BuildConnectionString("postgres", pooling: false));
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection)
            {
                CommandTimeout = (int)MaintenanceCommandTimeout.TotalSeconds
            };

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException exception)
                when (ignoreMissingDatabase && exception.SqlState == PostgresErrorCodes.InvalidCatalogName)
            {
            }
        }
        finally
        {
            definitionGate.Release();
        }
    }

    private string BuildConnectionString(string databaseName, bool pooling)
    {
        return new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = databaseName,
            Pooling = pooling
        }.ConnectionString;
    }

    private static async Task ReportFailureAsync(string step, Func<Task> cleanup)
    {
        try
        {
            await cleanup();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[postgres-runtime] cleanup step '{step}' failed: {exception.Message}");
        }
    }
}
