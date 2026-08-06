using System.Diagnostics;
using System.Text;
using DiagnosticsProcess = System.Diagnostics.Process;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

public sealed class PostgreSqlTestProcessContractTests
{
    private const string Owner = nameof(PostgreSqlTestProcessContractTests);
    private const string FlockExecutable = "/usr/bin/flock";
    private const string LocalDiagnosticConnection = "Host=localhost;Database=quran_dashboard;Username=reader";

    private static Func<string, string?> Variables(params (string Name, string Value)[] set)
    {
        var values = set.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
        return name => values.GetValueOrDefault(name);
    }

    [Fact]
    public async Task ConcurrentMigratedLeases_ShareOneServer_AndIsolateTheirData()
    {
        var leases = await Task.WhenAll(
            PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner),
            PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner));

        try
        {
            leases[1].ServerInstanceId.Should().Be(leases[0].ServerInstanceId);
            leases[0].ServerInstanceId.Should().NotBe(Guid.Empty);
            leases[0].DatabaseName.Should().NotBe(leases[1].DatabaseName);

            foreach (var lease in leases)
            {
                (await ScalarAsync(lease.ConnectionString, "SELECT count(*) FROM \"__EFMigrationsHistory\""))
                    .Should().BeOfType<long>().Which.Should().BeGreaterThan(0);
            }

            await ExecuteAsync(leases[0].ConnectionString, "CREATE TABLE lease_probe (id integer)");

            (await ScalarAsync(leases[0].ConnectionString, "SELECT to_regclass('public.lease_probe') IS NOT NULL"))
                .Should().Be(true);
            (await ScalarAsync(leases[1].ConnectionString, "SELECT to_regclass('public.lease_probe') IS NOT NULL"))
                .Should().Be(false);
        }
        finally
        {
            foreach (var lease in leases)
            {
                await lease.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task EmptyLease_CarriesNoMigratedSchema()
    {
        await using var lease = await PostgreSqlTestProcess.LeaseEmptyDatabaseAsync(Owner);

        (await ScalarAsync(lease.ConnectionString, "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NULL"))
            .Should().Be(true);
    }

    [Fact]
    public async Task DatabaseSlots_MakeAnExtraLeaseWait_UntilOneIsReleased()
    {
        var held = new List<PostgreSqlDatabaseLease>();
        try
        {
            while (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync() > 0)
            {
                held.Add(await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner));
            }

            held.Should().NotBeEmpty("draining the cap must take at least one slot");

            var blocked = PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
            var firstCompleted = await Task.WhenAny(blocked, Task.Delay(TimeSpan.FromSeconds(2)));
            firstCompleted.Should().NotBeSameAs(blocked, "the exhausted slot cap must hold the extra lease back");

            var released = held[^1];
            held.RemoveAt(held.Count - 1);
            await released.DisposeAsync();

            held.Add(await blocked.WaitAsync(TimeSpan.FromMinutes(1)));
        }
        finally
        {
            foreach (var lease in held)
            {
                await lease.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task CanceledCaller_IsRefused_AndLeavesTheRuntimeUsable()
    {
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        var lease = async () => await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, canceled.Token);

        await lease.Should().ThrowAsync<OperationCanceledException>();
        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync()).Should().Be(slotsBefore);

        await using var survivor = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        (await ScalarAsync(survivor.ConnectionString, "SELECT 1")).Should().Be(1);
    }

    [Fact]
    public async Task QueuedCaller_ThatIsCanceledWhileWaiting_LeavesTheSlotCountIntact()
    {
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();
        var held = new List<PostgreSqlDatabaseLease>();
        try
        {
            while (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync() > 0)
            {
                held.Add(await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner));
            }

            using var abandoned = new CancellationTokenSource();
            var queued = PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner, abandoned.Token);
            var firstCompleted = await Task.WhenAny(queued, Task.Delay(TimeSpan.FromSeconds(1)));
            firstCompleted.Should().NotBeSameAs(queued, "the caller must be queued before it is canceled");

            await abandoned.CancelAsync();
            var awaitQueued = async () => await queued;
            await awaitQueued.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            foreach (var lease in held)
            {
                await lease.DisposeAsync();
            }
        }

        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync())
            .Should().Be(slotsBefore, "a canceled waiter must not consume or leak a slot");
    }

    [Fact]
    public async Task LeaseDisposal_DropsTheDatabase_AndIsIdempotent()
    {
        await using var maintenance = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var slotsBefore = await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync();

        var lease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var databaseName = lease.DatabaseName;
        await lease.DisposeAsync();
        await lease.DisposeAsync();

        (await DatabaseExistsAsync(maintenance, databaseName)).Should().BeFalse();
        (await PostgreSqlTestProcess.AvailableDatabaseSlotsAsync()).Should().Be(slotsBefore);
    }

    [Fact]
    public async Task LeaseDisposal_ClearsOnlyItsOwnConnectionPool()
    {
        await using var survivor = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var pooledBackend = await ScalarAsync(survivor.ConnectionString, "SELECT pg_backend_pid()");

        var released = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        await ExecuteAsync(released.ConnectionString, "SELECT 1");
        await released.DisposeAsync();

        (await ScalarAsync(survivor.ConnectionString, "SELECT pg_backend_pid()"))
            .Should().Be(
                pooledBackend,
                "a lease clears only its own pool; clearing every pool would drop the physical connections "
                + "of collections that are still running");
    }

    [Fact]
    public async Task ExternalReadOnlyLease_LeavesItsDatabaseIntact_WhenDisposed()
    {
        await using var owned = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        await ExecuteAsync(
            owned.ConnectionString,
            "CREATE TABLE external_probe (id integer); INSERT INTO external_probe VALUES (7)");

        var external = PostgreSqlTestProcess.UseExternalReadOnlyDatabase(owned.ConnectionString);
        external.IsExternal.Should().BeTrue();
        external.ServerInstanceId.Should().Be(Guid.Empty);
        await external.DisposeAsync();

        (await ScalarAsync(owned.ConnectionString, "SELECT count(*) FROM external_probe"))
            .Should().Be(1L);
    }

    [Fact]
    public void UseExternalReadOnlyDatabase_RefusesANonLocalHost()
    {
        var external = () => PostgreSqlTestProcess.UseExternalReadOnlyDatabase(
            "Host=quran-dashboard.staging.internal;Database=quran_dashboard;Username=reader");

        external.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a loopback or local host*");
    }

    [Fact]
    public void FeatureDatabaseOverride_IsRefused_WithoutTheReadOnlyOptIn()
    {
        var resolve = () => ExternalReadOnlyDatabaseOptIn.ResolveConnectionString(
            ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable,
            Variables((ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable, LocalDiagnosticConnection)));

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{ExternalReadOnlyDatabaseOptIn.ModeVariable}={ExternalReadOnlyDatabaseOptIn.AcknowledgedMode}*");
    }

    [Fact]
    public void FeatureDatabaseOverride_IsHonoured_WithTheReadOnlyOptIn()
    {
        var resolved = ExternalReadOnlyDatabaseOptIn.ResolveConnectionString(
            ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable,
            Variables(
                (ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable, LocalDiagnosticConnection),
                (ExternalReadOnlyDatabaseOptIn.ModeVariable, ExternalReadOnlyDatabaseOptIn.AcknowledgedMode)));

        resolved.Should().Be(LocalDiagnosticConnection);
    }

    [Fact]
    public void FeatureDatabaseOverride_IsIgnored_WhenOnlyAnotherFeatureIsAcknowledged()
    {
        var resolved = ExternalReadOnlyDatabaseOptIn.ResolveConnectionString(
            ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable,
            Variables(
                (ExternalReadOnlyDatabaseOptIn.WordTypesConnectionVariable, LocalDiagnosticConnection),
                (ExternalReadOnlyDatabaseOptIn.ModeVariable, ExternalReadOnlyDatabaseOptIn.AcknowledgedMode)));

        resolved.Should().BeNull("an unset override leaves its fixture on an owned migrated lease");
    }

    [Fact]
    public void FeatureDatabaseOverride_IsRefused_WhenTwoFeaturesAreSetTogether()
    {
        var resolve = () => ExternalReadOnlyDatabaseOptIn.ResolveConnectionString(
            ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable,
            Variables(
                (ExternalReadOnlyDatabaseOptIn.MushafReaderConnectionVariable, LocalDiagnosticConnection),
                (ExternalReadOnlyDatabaseOptIn.RootsExplorerConnectionVariable, LocalDiagnosticConnection),
                (ExternalReadOnlyDatabaseOptIn.ModeVariable, ExternalReadOnlyDatabaseOptIn.AcknowledgedMode)));

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{ExternalReadOnlyDatabaseOptIn.RootsExplorerConnectionVariable}*");
    }

    [Fact]
    public async Task ExclusiveServerLease_IsRefused_WhileTheSharedRuntimeIsActive()
    {
        await using var shared = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);

        var exclusive = async () =>
            await PostgreSqlTestProcess.LeaseExclusiveServerAsync(Owner, "postgres:18-alpine");

        await exclusive.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*never run at once*");
    }

    [Fact]
    public async Task SchemaLease_IsolatesItsObjects_AndDropsThemOnDisposal()
    {
        await using var database = await PostgreSqlTestProcess.LeaseEmptyDatabaseAsync(Owner);
        var schema = await PostgreSqlSchemaLease.CreateAsync(database, Owner);

        await ExecuteAsync(schema.ConnectionString, "CREATE TABLE schema_probe (id integer)");

        (await ScalarAsync(
                database.ConnectionString,
                $"SELECT to_regclass('{schema.SchemaName}.schema_probe') IS NOT NULL"))
            .Should().Be(true);
        (await ScalarAsync(database.ConnectionString, "SELECT to_regclass('public.schema_probe') IS NULL"))
            .Should().Be(true);

        await schema.DisposeAsync();

        (await ScalarAsync(
                database.ConnectionString,
                $"SELECT count(*) FROM information_schema.schemata WHERE schema_name = '{schema.SchemaName}'"))
            .Should().Be(0L);
    }

    [Fact]
    public void GeneratedDatabaseNames_AreUnique_AndFitPostgreSqlIdentifiers()
    {
        var names = Enumerable.Range(0, 64)
            .Select(_ => PostgreSqlDatabaseName.CreateForOwner(
                "Quran.WordsMorphologyExplorers MorphologyExplorersTestFixture (real import)"))
            .ToArray();

        names.Should().OnlyHaveUniqueItems();
        names.Should().OnlyContain(name =>
            Encoding.UTF8.GetByteCount(name) <= PostgreSqlDatabaseName.MaximumLength);
        names.Should().OnlyContain(name =>
            name.StartsWith(PostgreSqlDatabaseName.Prefix, StringComparison.Ordinal));
        PostgreSqlDatabaseName.Quote("weird\"name").Should().Be("\"weird\"\"name\"");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("-1")]
    [InlineData("four")]
    public void DatabaseParallelism_RejectsValuesOutsideTheReviewedRange(string configured)
    {
        var resolve = () => PostgreSqlTestServer.ResolveDatabaseParallelism(configured);

        resolve.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{PostgreSqlTestServer.DatabaseParallelismVariable}*");
    }

    [Theory]
    [InlineData(null, PostgreSqlTestServer.MaximumDatabaseParallelism)]
    [InlineData("", PostgreSqlTestServer.MaximumDatabaseParallelism)]
    [InlineData("1", 1)]
    [InlineData("4", 4)]
    public void DatabaseParallelism_AcceptsOneToFour_AndDefaultsToFour(string? configured, int expected)
    {
        PostgreSqlTestServer.ResolveDatabaseParallelism(configured).Should().Be(expected);
    }

    [Fact]
    public void ProjectLockFilePath_IsScopedToTheTestProjectAndUser()
    {
        var path = CrossProcessPostgreSqlLock.ProjectLockFilePath;

        Path.GetDirectoryName(path).Should().EndWith("quran-dashboard-tests");
        Path.GetFileName(path).Should().MatchRegex("^[0-9a-f]{16}-postgres\\.lock$");
    }

    [Fact]
    public async Task CrossProcessLock_WaitsForAnotherHoldingProcess_ThenAcquires()
    {
        File.Exists(FlockExecutable).Should()
            .BeTrue($"cross-process lock coverage requires {FlockExecutable}");

        var lockPath = Path.Combine(
            Path.GetTempPath(),
            "quran-dashboard-tests",
            $"contract-{Guid.NewGuid():N}-postgres.lock");
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);

        using var holder = DiagnosticsProcess.Start(new ProcessStartInfo(FlockExecutable)
        {
            ArgumentList = { "--exclusive", lockPath, "--command", "sleep 6" }
        })!;

        (await WaitUntilHeldAsync(lockPath)).Should()
            .BeTrue("the child process must take the lock before the wait is measured");

        var waited = Stopwatch.StartNew();
        using (await CrossProcessPostgreSqlLock.AcquireAsync(
            Owner,
            lockPath,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(200)))
        {
            waited.Stop();
        }

        await holder.WaitForExitAsync();
        waited.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CrossProcessLock_TimesOutWithANamedHolder_RatherThanWaitingForever()
    {
        var lockPath = Path.Combine(
            Path.GetTempPath(),
            "quran-dashboard-tests",
            $"contract-{Guid.NewGuid():N}-postgres.lock");
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);

        using var holder = await CrossProcessPostgreSqlLock.AcquireAsync(
            "first holder",
            lockPath,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100));

        var second = async () => await CrossProcessPostgreSqlLock.AcquireAsync(
            "second holder",
            lockPath,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(100));

        (await second.Should().ThrowAsync<TimeoutException>())
            .WithMessage("*first holder*");
    }

    private static async Task<bool> WaitUntilHeldAsync(string lockPath)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(10))
        {
            try
            {
                using var probe = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        return false;
    }

    private static async Task<bool> DatabaseExistsAsync(PostgreSqlDatabaseLease lease, string databaseName)
    {
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder(lease.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(maintenanceConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_database WHERE datname = @name",
            connection);
        command.Parameters.AddWithValue("name", databaseName);

        return (long)(await command.ExecuteScalarAsync())! > 0;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
