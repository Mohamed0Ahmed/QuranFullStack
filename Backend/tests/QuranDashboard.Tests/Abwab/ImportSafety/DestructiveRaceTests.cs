using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

// US2 / FR-005..006, SC-001: a destructive Quran import must fail closed when an out-of-scope table
// (standing in for a future Abwab dependent) references a Quran target, even under concurrent
// creation of that dependent. Proven against real PostgreSQL.
//
// Fixture note (mandatory-order constraint): US2 runs BEFORE the kernel/schema (US3), so no real
// Abwab table/entity/FK exists yet and none is created here. The dependent is a TEST-ONLY, clearly
// synthetic table created via raw SQL (never an EF entity, so the T006 FK-prohibition guard — which
// reflects the EF model — stays green) and carries NO real ayah key.
[Collection(nameof(AbwabDbCollection))]
public sealed class DestructiveRaceTests
{
    // Names an out-of-scope (non `quran_`) table so the closure preflight classifies it as a dependent
    // that a TRUNCATE ... CASCADE would silently destroy — exactly the Abwab data-loss it must prevent.
    private const string SyntheticDependentTable = "abwab_synthetic_dependent_us2";

    private const string FoundationTruncateSql =
        "TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE";

    private readonly PostgresFixture _fixture;

    public DestructiveRaceTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Preflight_FailsClosed_WhenOutOfScopeDependentReferencesQuran()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        try
        {
            await DropSyntheticDependentAsync(connection);
            await CreateSyntheticDependentAsync(connection);

            await using var transaction = await connection.BeginTransactionAsync();

            Func<Task> act = () => QuranImportDestructiveGuard.EnsureNoOutOfScopeDependentsAsync(
                connection, transaction, FoundationTruncateSql, CancellationToken.None);

            (await act.Should().ThrowAsync<QuranImportSafetyException>(
                "a TRUNCATE ... CASCADE that would reach an out-of-scope dependent must fail closed"))
                .Which.Message.Should().Contain(SyntheticDependentTable);

            await transaction.RollbackAsync();
        }
        finally
        {
            await DropSyntheticDependentAsync(connection);
        }
    }

    // Non-vacuous companion: with no out-of-scope dependent, the current all-Quran truncation set is
    // closed, so the preflight must PASS — otherwise it would falsely block every real Quran import.
    [Fact]
    public async Task Preflight_Passes_WhenTruncationSetIsClosed()
    {
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await DropSyntheticDependentAsync(connection);

        await using var transaction = await connection.BeginTransactionAsync();

        var act = () => QuranImportDestructiveGuard.EnsureNoOutOfScopeDependentsAsync(
            connection, transaction, FoundationTruncateSql, CancellationToken.None);

        await act.Should().NotThrowAsync();

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ConcurrentDependentCreation_BlocksDestructiveImport_ThenFailsClosed_DependentPreserved()
    {
        await using var creator = new NpgsqlConnection(_fixture.ConnectionString);
        await using var importer = new NpgsqlConnection(_fixture.ConnectionString);
        await creator.OpenAsync();
        await importer.OpenAsync();

        try
        {
            await DropSyntheticDependentAsync(creator);

            // The dependent creator takes the destructive lock first and begins creating the dependent.
            await using var creatorTransaction = await creator.BeginTransactionAsync();
            await QuranImportDestructiveGuard.AcquireDestructiveLockAsync(
                creator, creatorTransaction, CancellationToken.None);

            // The destructive import must BLOCK on the same lock while the dependent is being created.
            await using var importerTransaction = await importer.BeginTransactionAsync();
            var importerLock = QuranImportDestructiveGuard.AcquireDestructiveLockAsync(
                importer, importerTransaction, CancellationToken.None);

            var completedFirst = await Task.WhenAny(importerLock, Task.Delay(1500));
            completedFirst.Should().NotBeSameAs(
                importerLock,
                "the destructive import must serialize behind the in-flight dependent creation, not race it");

            // The dependent finishes being created and the lock is released on commit.
            await CreateSyntheticDependentAsync(creator, creatorTransaction);
            await creatorTransaction.CommitAsync();

            await importerLock; // now the importer acquires the lock

            Func<Task> importStep = () => QuranImportDestructiveGuard.EnsureNoOutOfScopeDependentsAsync(
                importer, importerTransaction, FoundationTruncateSql, CancellationToken.None);

            await importStep.Should().ThrowAsync<QuranImportSafetyException>(
                "once the dependent exists, the destructive import must fail closed rather than cascade-destroy it");

            await importerTransaction.RollbackAsync();

            (await SyntheticDependentExistsAsync(importer)).Should().BeTrue(
                "the concurrently created dependent must be preserved, never lost to the destructive import");
        }
        finally
        {
            await DropSyntheticDependentAsync(importer);
        }
    }

    [Fact]
    public async Task Preflight_FailsClosed_WhenDestructiveStatementHasNoParseableTargets()
    {
        // A quoted / mixed-case identifier the target parser cannot recognize yields zero parsed
        // targets; the guard must refuse the statement rather than run it unpreflighted (fail closed,
        // not open) — otherwise a schema-qualified/quoted/CTE destructive statement could cascade freely.
        const string unparseableDestructiveSql = "TRUNCATE \"AbwabDependent\" RESTART IDENTITY CASCADE";

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        Func<Task> act = () => QuranImportDestructiveGuard.EnsureNoOutOfScopeDependentsAsync(
            connection, transaction, unparseableDestructiveSql, CancellationToken.None);

        await act.Should().ThrowAsync<QuranImportSafetyException>(
            "a destructive statement with no parseable target cannot be closure-checked and must fail closed");

        await transaction.RollbackAsync();
    }

    private static async Task CreateSyntheticDependentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(
            $"CREATE TABLE {SyntheticDependentTable} ("
            + " id integer PRIMARY KEY,"
            + " surah_number integer NOT NULL REFERENCES quran_surahs(surah_number))",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropSyntheticDependentAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            $"DROP TABLE IF EXISTS {SyntheticDependentTable}", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> SyntheticDependentExistsAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT to_regclass('{SyntheticDependentTable}') IS NOT NULL", connection);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
