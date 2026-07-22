using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Safety;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab.ImportSafety;

[Collection(nameof(AbwabDbCollection))]
public sealed class DestructiveRaceTests
{
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

            await using var creatorTransaction = await creator.BeginTransactionAsync();
            await QuranImportDestructiveGuard.AcquireDestructiveLockAsync(
                creator, creatorTransaction, CancellationToken.None);

            await using var importerTransaction = await importer.BeginTransactionAsync();
            var importerLock = QuranImportDestructiveGuard.AcquireDestructiveLockAsync(
                importer, importerTransaction, CancellationToken.None);

            var completedFirst = await Task.WhenAny(importerLock, Task.Delay(1500));
            completedFirst.Should().NotBeSameAs(
                importerLock,
                "the destructive import must serialize behind the in-flight dependent creation, not race it");

            await CreateSyntheticDependentAsync(creator, creatorTransaction);
            await creatorTransaction.CommitAsync();

            await importerLock;

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
