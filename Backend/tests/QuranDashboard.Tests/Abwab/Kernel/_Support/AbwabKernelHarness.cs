using Microsoft.EntityFrameworkCore.Diagnostics;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Abwab.Caching;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.Kernel._Support;

// Shared helpers for the real-PG kernel tests: context construction on the migrated fixture DB, the
// throwaway auditable-fixture table, and a pristine-state reset (the audit tables are append-only, so the
// reset uses session_replication_role=replica to bypass the DB triggers for test teardown only).
internal static class AbwabKernelHarness
{
    public const string FixtureTable = "abwab_kernel_fixture";

    public static QuranDashboardDbContext CreateProductionContext(
        PostgresFixture fixture,
        ISaveChangesInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<QuranDashboardDbContext>().UseNpgsql(fixture.ConnectionString);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new QuranDashboardDbContext(builder.Options);
    }

    public static AbwabKernelTestContext CreateGuardedContext(PostgresFixture fixture, AbwabPersonalDeletePolicy policy)
    {
        var options = new DbContextOptionsBuilder<AbwabKernelTestContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new AbwabWriteGuardInterceptor(policy))
            .Options;

        return new AbwabKernelTestContext(options);
    }

    public static async Task ResetKernelStateAsync(PostgresFixture fixture)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        // replica mode disables the append-only/root-protection triggers for this session only, so the
        // test harness can wipe the append-only tables between tests. Production never does this.
        await ExecuteAsync(connection, "SET session_replication_role = replica");
        await ExecuteAsync(connection, "TRUNCATE abwab_audit_events, abwab_change_sets");
        await ExecuteAsync(connection, "DELETE FROM abwab_timeline_generation_boundaries WHERE is_root = false");
        await ExecuteAsync(
            connection,
            "UPDATE abwab_revision_state SET audit_head_sequence = 0, timeline_generation = 0, tree_revision = 0 WHERE id = 1");
        await ExecuteAsync(connection, "UPDATE abwab_write_barrier SET state = 0 WHERE id = 1");
        await ExecuteAsync(connection, "SET session_replication_role = origin");
    }

    public static async Task EnsureFixtureTableAsync(PostgresFixture fixture)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(
            connection,
            $"CREATE TABLE IF NOT EXISTS {FixtureTable} "
            + "(id uuid PRIMARY KEY, label text NOT NULL, is_deleted boolean NOT NULL DEFAULT false)");
        await ExecuteAsync(connection, $"DELETE FROM {FixtureTable}");
    }

    // Runs one audited commit against the real executor on a FRESH context/connection, so multi-commit and
    // concurrency tests never share EF tracking state.
    public static async Task<AbwabCommitResult> CommitAsync(
        PostgresFixture fixture,
        AbwabWriteRequest request,
        IAbwabCachePublisher? publisher = null,
        IServerClock? clock = null)
    {
        await using var context = CreateProductionContext(fixture);
        var executor = new AbwabAuditedCommitExecutor(
            context,
            clock ?? new FixedServerClock(DateTimeOffset.UnixEpoch),
            publisher ?? new NullAbwabCachePublisher());

        return await executor.ExecuteAsync(request, CancellationToken.None);
    }

    public static AbwabWriteRequest Request(long expectedGeneration, int eventOrdinal = 0, string actor = "tester") =>
        new(ExpectedTimelineGeneration.Of(expectedGeneration), actor, [new AbwabAuditEventDraft(eventOrdinal, "fixture-event")]);

    public static async Task<long> ReadAuditHeadAsync(PostgresFixture fixture)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT audit_head_sequence FROM abwab_revision_state WHERE id = 1";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    public static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
