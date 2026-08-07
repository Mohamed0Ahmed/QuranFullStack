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
                (await PostgreSqlContractProbe.ScalarAsync(
                        lease.ConnectionString,
                        "SELECT count(*) FROM \"__EFMigrationsHistory\""))
                    .Should().BeOfType<long>().Which.Should().BeGreaterThan(0);
            }

            await PostgreSqlContractProbe.ExecuteAsync(
                leases[0].ConnectionString,
                "CREATE TABLE lease_probe (id integer)");

            (await PostgreSqlContractProbe.ScalarAsync(
                    leases[0].ConnectionString,
                    "SELECT to_regclass('public.lease_probe') IS NOT NULL"))
                .Should().Be(true);
            (await PostgreSqlContractProbe.ScalarAsync(
                    leases[1].ConnectionString,
                    "SELECT to_regclass('public.lease_probe') IS NOT NULL"))
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

        (await PostgreSqlContractProbe.ScalarAsync(
                lease.ConnectionString,
                "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NULL"))
            .Should().Be(true);
    }

    [Fact]
    public async Task LeaseDisposal_ClearsOnlyItsOwnConnectionPool()
    {
        await using var survivor = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        var pooledBackend = await PostgreSqlContractProbe.ScalarAsync(
            survivor.ConnectionString,
            "SELECT pg_backend_pid()");

        var released = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        await PostgreSqlContractProbe.ExecuteAsync(released.ConnectionString, "SELECT 1");
        await released.DisposeAsync();

        (await PostgreSqlContractProbe.ScalarAsync(survivor.ConnectionString, "SELECT pg_backend_pid()"))
            .Should().Be(
                pooledBackend,
                "a lease clears only its own pool; clearing every pool would drop the physical connections "
                + "of collections that are still running");
    }

    [Fact]
    public async Task ExternalReadOnlyLease_LeavesItsDatabaseIntact_WhenDisposed()
    {
        await using var owned = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(Owner);
        await PostgreSqlContractProbe.ExecuteAsync(
            owned.ConnectionString,
            "CREATE TABLE external_probe (id integer); INSERT INTO external_probe VALUES (7)");

        var external = PostgreSqlTestProcess.UseExternalReadOnlyDatabase(owned.ConnectionString);
        external.IsExternal.Should().BeTrue();
        external.ServerInstanceId.Should().Be(Guid.Empty);
        await external.DisposeAsync();

        (await PostgreSqlContractProbe.ScalarAsync(owned.ConnectionString, "SELECT count(*) FROM external_probe"))
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

        await PostgreSqlContractProbe.ExecuteAsync(
            schema.ConnectionString,
            "CREATE TABLE schema_probe (id integer)");

        (await PostgreSqlContractProbe.ScalarAsync(
                database.ConnectionString,
                $"SELECT to_regclass('{schema.SchemaName}.schema_probe') IS NOT NULL"))
            .Should().Be(true);
        (await PostgreSqlContractProbe.ScalarAsync(
                database.ConnectionString,
                "SELECT to_regclass('public.schema_probe') IS NULL"))
            .Should().Be(true);

        await schema.DisposeAsync();

        (await PostgreSqlContractProbe.ScalarAsync(
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
}
