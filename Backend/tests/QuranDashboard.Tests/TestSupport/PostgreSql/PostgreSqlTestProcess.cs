namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class PostgreSqlTestProcess
{
    private static readonly TimeSpan ProcessExitCleanupBudget = TimeSpan.FromSeconds(30);

    private static readonly Lazy<Task<PostgreSqlTestServer>> ServerLoader = new(
        () => PostgreSqlTestServer.StartAsync(CancellationToken.None),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static int exclusiveLeases;

    static PostgreSqlTestProcess()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanUpOnProcessExit();
    }

    internal static bool SharedServerRequested => ServerLoader.IsValueCreated;

    internal static async Task<int> AvailableDatabaseSlotsAsync(CancellationToken cancellationToken = default)
    {
        var server = await GetServerAsync(cancellationToken);
        return server.AvailableDatabaseSlots;
    }

    internal static async Task<int> ConfiguredDatabaseSlotsAsync(CancellationToken cancellationToken = default)
    {
        var server = await GetServerAsync(cancellationToken);
        return server.ConfiguredDatabaseSlots;
    }

    internal static async Task<PostgreSqlDatabaseLease> LeaseMigratedDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        var server = await GetServerAsync(cancellationToken);
        return await server.LeaseMigratedDatabaseAsync(owner, cancellationToken);
    }

    internal static async Task<PostgreSqlDatabaseLease> LeaseEmptyDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        var server = await GetServerAsync(cancellationToken);
        return await server.LeaseEmptyDatabaseAsync(owner, cancellationToken);
    }

    internal static PostgreSqlDatabaseLease UseExternalReadOnlyDatabase(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException(
                "An external read-only database must name the database explicitly.");
        }

        if (!IsLocalHost(builder.Host))
        {
            throw new InvalidOperationException(
                $"An external read-only database must be local; '{builder.Host}' is not a loopback or local host. "
                + "Remote, shared, staging, and production targets are refused.");
        }

        return PostgreSqlDatabaseLease.External(builder.Database!, connectionString);
    }

    internal static async Task<ExclusivePostgreSqlLease> LeaseExclusiveServerAsync(
        string owner,
        string image,
        Func<PostgreSqlBuilder, PostgreSqlBuilder>? configureContainer = null,
        CancellationToken cancellationToken = default)
    {
        if (SharedServerRequested)
        {
            throw new InvalidOperationException(
                $"The shared {PostgreSqlTestServer.Image} runtime is already active in this test process, and it "
                + "stays alive until process exit. Two project-owned PostgreSQL containers must never run at once, "
                + "so an exclusive server belongs in its own test process.");
        }

        Interlocked.Increment(ref exclusiveLeases);
        try
        {
            return await ExclusivePostgreSqlLease.AcquireAsync(
                owner,
                image,
                () => Interlocked.Decrement(ref exclusiveLeases),
                configureContainer,
                cancellationToken);
        }
        catch
        {
            Interlocked.Decrement(ref exclusiveLeases);
            throw;
        }
    }

    private static Task<PostgreSqlTestServer> GetServerAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref exclusiveLeases) != 0)
        {
            throw new InvalidOperationException(
                "An exclusive PostgreSQL server is active in this test process; the shared runtime would be a "
                + "second concurrent project-owned container.");
        }

        return ServerLoader.Value.WaitAsync(cancellationToken);
    }

    private static bool IsLocalHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (host.StartsWith('/'))
        {
            return true;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }

    private static void CleanUpOnProcessExit()
    {
        if (!ServerLoader.IsValueCreated)
        {
            return;
        }

        try
        {
            var startup = ServerLoader.Value;
            if (!startup.Wait(ProcessExitCleanupBudget) || !startup.IsCompletedSuccessfully)
            {
                return;
            }

            startup.Result.DisposeAsync().AsTask().Wait(ProcessExitCleanupBudget);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[postgres-runtime] process-exit cleanup failed: {exception.Message}");
        }
    }
}
