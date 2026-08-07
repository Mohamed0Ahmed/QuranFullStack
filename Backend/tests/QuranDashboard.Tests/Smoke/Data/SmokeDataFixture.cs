using DotNet.Testcontainers.Configurations;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke.Data;

// The same Testing-environment host as the pipeline tier (SmokeApiHost), over a server seeded with the
// canonical dump instead of an empty schema. Its own server, never SmokeApiFixture's: the pipeline
// sweep's expectations are derived against a migrated-but-EMPTY schema, so seeding the database it reads
// would invalidate them.
//
// The server is an EXCLUSIVE lease, taken through PostgreSqlTestProcess so it holds the same
// CrossProcessPostgreSqlLock the shared postgres:16-alpine runtime holds. Two project-owned PostgreSQL
// containers must never run at once, and this is the one fixture that cannot join the shared runtime, so
// the lock is what makes "exclusive" true across processes and LeaseExclusiveServerAsync is what makes it
// true within one. Backend/scripts/test-backend completes the guarantee by running any lane that selects
// both this class and a shared-runtime class as two sequential invocations.
public sealed class SmokeDataFixture : IAsyncLifetime
{
    private const string DumpMountPath = "/dump";

    // postgres:18-alpine, where every sibling fixture (AccessTestFixture, SmokeApiFixture) runs 16-alpine:
    // the dump is written by pg_dump 18, and a pg16 pg_restore rejects a newer archive header outright.
    // The image tracks the producer, which is why the major is a named constant the gate checks the
    // manifest against rather than a number buried in the image tag.
    private const int RestoreImageMajorVersion = 18;
    private static readonly string RestoreImage = $"postgres:{RestoreImageMajorVersion}-alpine";

    private readonly FakeExternalUserProfileSource _profileSource = new();
    private readonly SmokeSqlCommandCapture _commandCapture = new();

    private ExclusivePostgreSqlLease? _serverLease;
    private WebApplicationFactory<HealthController>? _apiFactory;
    private SmokeDumpManifest? _manifest;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Absent is the ordinary state on a machine without the artifact: every test in the tier is
        // already skipped by SmokeDumpFact/SmokeDumpTheory, so start nothing. xUnit builds a collection
        // fixture even when all of its tests skip, which is why the check has to be here and not only in
        // the attributes.
        if (SmokeDumpGate.IsAbsent)
        {
            return;
        }

        // Before the lock and any container work, so a stale dump — or one this image cannot restore —
        // costs a hash rather than a two-minute pull, start and mid-restore failure.
        _manifest = SmokeDumpGate.VerifyAndRead(RestoreImageMajorVersion);

        _serverLease = await PostgreSqlTestProcess.LeaseExclusiveServerAsync(
            nameof(SmokeDataFixture),
            RestoreImage,
            // Bind-mounted read-only rather than copied in: the archive is ~350 MB, and copying streams
            // every byte through the Docker API before the restore can begin.
            builder => builder.WithBindMount(SmokeDumpGate.DumpDirectory, DumpMountPath, AccessMode.ReadOnly));
        ConnectionString = _serverLease.ConnectionString;

        // The dump is data-only, so the schema it lands in is this tree's migrations, applied first.
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new QuranDashboardDbContext(options))
        {
            await context.Database.MigrateAsync();
        }

        await RestoreDumpAsync();

        _apiFactory = SmokeApiHost.Build(ConnectionString, _profileSource, _commandCapture);
    }

    // Ordered: the host and its pool first, then the container, and the cross-process lock last — the next
    // test process may not start a PostgreSQL container until this one is gone. Only this fixture's own
    // pool is cleared; ClearAllPools would reach collections that are still running.
    public async Task DisposeAsync()
    {
        _apiFactory?.Dispose();
        _apiFactory = null;

        if (_serverLease is not null)
        {
            NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionString));
            await _serverLease.DisposeAsync();
            _serverLease = null;
        }
    }

    public HttpClient CreateClient() => SmokeApiHost.CreateClient(Factory);

    internal SmokeDumpManifest Manifest => _manifest
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeDataFixture)} has no manifest. The canonical dump is absent, so every test in this tier should have been skipped.");

    // Counted straight from the restored database rather than through a route, so a short restore is
    // reported as a short restore instead of surfacing as a puzzling count mismatch on a read endpoint.
    // Every table shares one connection: the caller wants the whole manifest, and a connect/close cycle
    // per table is one round trip per table for a single assertion.
    internal async Task<IReadOnlyDictionary<string, int>> CountRowsAsync(IEnumerable<string> tables)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var counts = new Dictionary<string, int>();
        foreach (var table in tables)
        {
            // Interpolated because a table name cannot be a parameter. The value is a key of a manifest
            // written by create-smoke-dump from pg_class on a local operator machine — nothing verifies
            // the manifest's own contents, so this is trusted as a local file, not as validated input.
            await using var command = new NpgsqlCommand($"SELECT count(*) FROM public.\"{table}\";", connection);
            counts[table] = Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        return counts;
    }

    private WebApplicationFactory<HealthController> Factory => _apiFactory
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeDataFixture)} has not been initialized. Ensure it is used as an ICollectionFixture.");

    private async Task RestoreDumpAsync()
    {
        var connection = new NpgsqlConnectionStringBuilder(ConnectionString);

        // Run inside the container, against the mounted archive, so no part of 350 MB crosses the Docker
        // API twice.
        //
        // --jobs rather than --single-transaction: pg_restore refuses the two together, and the restore
        // dominates this tier's runtime. The transaction is not what protects this tier — the throw below
        // is: it happens during InitializeAsync, so a restore that reports failure is never handed to a
        // test at all. SmokeDataReadTests then re-counts every table against the manifest, covering the
        // one case this cannot see, an archive that exits zero having applied less than all of itself.
        // --disable-triggers is what lets the data land with the migrated foreign keys already in place;
        // it works because the container's postgres user is a superuser.
        var result = await _serverLease!.ExecAsync([
            "pg_restore",
            "--username", connection.Username!,
            "--dbname", connection.Database!,
            "--data-only",
            "--disable-triggers",
            "--jobs", "4",
            $"{DumpMountPath}/{SmokeDumpGate.DumpFileName}",
        ]);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_restore of the canonical smoke dump failed with exit code {result.ExitCode}. An " +
                $"unsupported-archive-version message means the dump was written by a newer pg_dump than " +
                $"this fixture's postgres image. stderr: {result.Stderr}");
        }
    }
}
