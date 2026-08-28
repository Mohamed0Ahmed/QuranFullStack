using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Infrastructure.Persistence.Linking;
using QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;

public sealed class SqlDisplayWordsRebuilder : IDisplayWordsRebuilder
{
    private const int CommandTimeoutSeconds = 600;

    private const string AnyTargetTableHasDataSql =
        """
        SELECT EXISTS (SELECT 1 FROM quran_words_ordered_tashkeel)
            OR EXISTS (SELECT 1 FROM quran_words_ordered_simple)
            OR EXISTS (SELECT 1 FROM quran_words_unique_tashkeel)
            OR EXISTS (SELECT 1 FROM quran_words_unique_simple)
        """;

    private const string ResetOrderedTablesSql =
        """
        DELETE FROM quran_words_ordered_tashkeel;
        DELETE FROM quran_words_ordered_simple;
        """;

    private const string UpsertUniqueTashkeelSql = DisplayWordsSql.InsertUniqueTashkeel + "\n" + """
        ON CONFLICT (id) DO UPDATE SET
          text_uthmani = EXCLUDED.text_uthmani,
          text_uthmani_simple = EXCLUDED.text_uthmani_simple,
          text_imlaei_simple = EXCLUDED.text_imlaei_simple,
          occurrences_count = EXCLUDED.occurrences_count,
          ayahs_count = EXCLUDED.ayahs_count,
          surahs_count = EXCLUDED.surahs_count,
          first_quran_word_id = EXCLUDED.first_quran_word_id,
          first_location = EXCLUDED.first_location,
          first_surah_number = EXCLUDED.first_surah_number,
          first_ayah_number = EXCLUDED.first_ayah_number,
          first_word_order_in_mushaf = EXCLUDED.first_word_order_in_mushaf,
          first_page_number = EXCLUDED.first_page_number,
          first_line_number = EXCLUDED.first_line_number
        WHERE quran_words_unique_tashkeel.text_uthmani = EXCLUDED.text_uthmani
        """;

    private const string UpsertUniqueSimpleSql = DisplayWordsSql.InsertUniqueSimple + "\n" + """
        ON CONFLICT (id) DO UPDATE SET
          word_key_imlaei_simple = EXCLUDED.word_key_imlaei_simple,
          text_uthmani = EXCLUDED.text_uthmani,
          text_uthmani_simple = EXCLUDED.text_uthmani_simple,
          text_imlaei_simple = EXCLUDED.text_imlaei_simple,
          qpc_glyph = EXCLUDED.qpc_glyph,
          occurrences_count = EXCLUDED.occurrences_count,
          ayahs_count = EXCLUDED.ayahs_count,
          surahs_count = EXCLUDED.surahs_count,
          first_quran_word_id = EXCLUDED.first_quran_word_id,
          first_location = EXCLUDED.first_location,
          first_surah_number = EXCLUDED.first_surah_number,
          first_ayah_number = EXCLUDED.first_ayah_number,
          first_word_order_in_mushaf = EXCLUDED.first_word_order_in_mushaf,
          first_page_number = EXCLUDED.first_page_number,
          first_line_number = EXCLUDED.first_line_number
        WHERE quran_words_unique_simple.word_key_imlaei_simple = EXCLUDED.word_key_imlaei_simple
        """;

    private const string RemoveObsoleteUniqueRowsSql = QuranTashkeelIdentitySql.IdentityCte +
        """
        , current_tashkeel AS (
          SELECT DISTINCT ON (
                   btrim(translate(word.text_uthmani, identity.ignored_tashkeel_marks, '')))
                 word.id,
                 word.text_uthmani
          FROM quran_words AS word
          CROSS JOIN display_word_identity identity
          WHERE word.is_ayah_marker = false
          ORDER BY btrim(translate(word.text_uthmani, identity.ignored_tashkeel_marks, '')), word.id
        )
        DELETE FROM quran_words_unique_tashkeel AS unique_word
        WHERE NOT EXISTS (
          SELECT 1
          FROM current_tashkeel AS current_word
          WHERE current_word.id = unique_word.id
            AND current_word.text_uthmani = unique_word.text_uthmani
        )
          AND NOT EXISTS (
            SELECT 1
            FROM linking_workspace_sources AS source
            WHERE source.unique_tashkeel_word_id = unique_word.id
               OR source.word_type_tashkeel_word_id = unique_word.id
          )
          AND NOT EXISTS (
            SELECT 1
            FROM linking_source_contributions AS contribution
            WHERE contribution.unique_tashkeel_word_id = unique_word.id
               OR contribution.word_type_tashkeel_word_id = unique_word.id
          );

        DELETE FROM quran_words_unique_simple AS unique_word
        WHERE NOT EXISTS (
          SELECT 1
          FROM quran_words AS word
          WHERE word.is_ayah_marker = false
            AND word.id = unique_word.id
            AND word.word_key_imlaei_simple = unique_word.word_key_imlaei_simple
        )
          AND NOT EXISTS (
            SELECT 1
            FROM linking_workspace_sources AS source
            WHERE source.unique_simple_word_id = unique_word.id
          )
          AND NOT EXISTS (
            SELECT 1
            FROM linking_source_contributions AS contribution
            WHERE contribution.unique_simple_word_id = unique_word.id
          );
        """;

    private readonly QuranDashboardDbContext dbContext;
    private readonly ILinkingDataRevisionWriterStore revisionStore;
    private readonly PhraseSourceStateCoordinator phraseSourceStateCoordinator;
    private readonly DisplayWordsRebuildVerifier verifier = new();

    public SqlDisplayWordsRebuilder(
        QuranDashboardDbContext dbContext,
        PhraseSourceStateCoordinator phraseSourceStateCoordinator,
        ILinkingDataRevisionWriterStore? revisionStore = null)
    {
        this.dbContext = dbContext;
        this.phraseSourceStateCoordinator = phraseSourceStateCoordinator;
        this.revisionStore = revisionStore ?? new LinkingDataRevisionStore();
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.QuranWordsOrderedTashkeel.AnyAsync(ct)
            || await dbContext.QuranWordsOrderedSimple.AnyAsync(ct)
            || await dbContext.QuranWordsUniqueTashkeel.AnyAsync(ct)
            || await dbContext.QuranWordsUniqueSimple.AnyAsync(ct);
    }

    public async Task<DisplayWordsRebuildResult> RebuildAsync(
        bool force,
        int expectedReadableWords,
        CancellationToken ct)
    {
        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State == ConnectionState.Open)
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        await connection.OpenAsync(ct);

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for display-words rebuild.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            await phraseSourceStateCoordinator.LockSourceMutationAsync(
                npgsqlConnection,
                transaction,
                ct);
            await revisionStore.LockForWriteAsync(npgsqlConnection, transaction, ct);

            if (!force && await AnyTargetTableHasDataAsync(npgsqlConnection, transaction, ct))
            {
                throw new InvalidOperationException(DisplayWordsInvariants.TargetsNotEmpty);
            }

            var sourceCountsBefore = await verifier.ReadSourceCountsAsync(
                npgsqlConnection,
                transaction,
                ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, DisplayWordsSql.NullQuranWordLinks, ct);

            if (force)
            {
                await ExecuteNonQueryAsync(npgsqlConnection, transaction, ResetOrderedTablesSql, ct);
            }

            await ExecuteNonQueryAsync(npgsqlConnection, transaction, DisplayWordsSql.InsertOrderedTashkeel, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, DisplayWordsSql.InsertOrderedSimple, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, UpsertUniqueTashkeelSql, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, UpsertUniqueSimpleSql, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, RemoveObsoleteUniqueRowsSql, ct);

            await ExecuteNonQueryAsync(npgsqlConnection, transaction, DisplayWordsSql.UpdateUniqueTashkeelLinks, ct);
            await ExecuteNonQueryAsync(npgsqlConnection, transaction, DisplayWordsSql.UpdateUniqueSimpleLinks, ct);

            var assessment = await verifier.VerifyAsync(
                npgsqlConnection,
                transaction,
                expectedReadableWords,
                sourceCountsBefore,
                ct);

            if (assessment.AllHardChecksPassed)
            {
                var phraseSource = await phraseSourceStateCoordinator.RefreshAfterDisplayRebuildAsync(
                    npgsqlConnection,
                    transaction,
                    expectedReadableWords,
                    ct);
                if (!phraseSource.Passed)
                {
                    await transaction.RollbackAsync(ct);
                    var phraseErrors = phraseSource.Checks
                        .Where(check => !check.Passed)
                        .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                        .ToList();
                    return verifier.BuildResult(
                        runAtUtc,
                        force,
                        assessment,
                        persisted: false,
                        phraseErrors);
                }

                await revisionStore.IncrementAsync(npgsqlConnection, transaction, ct);
                await transaction.CommitAsync(ct);

                return verifier.BuildResult(
                    runAtUtc,
                    force,
                    assessment,
                    persisted: true,
                    errors: []);
            }

            await transaction.RollbackAsync(ct);

            return verifier.BuildResult(
                runAtUtc,
                force,
                assessment,
                persisted: false,
                assessment.HardErrors);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<bool> AnyTargetTableHasDataAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(AnyTargetTableHasDataSql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

}
