using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;
using QuranDashboard.Infrastructure.Persistence.Linking;

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

    private const string RemoveObsoleteUniqueRowsSql =
        """
        DELETE FROM quran_words_unique_tashkeel AS unique_word
        WHERE NOT EXISTS (
          SELECT 1
          FROM quran_words AS word
          WHERE word.is_ayah_marker = false
            AND word.id = unique_word.id
            AND word.text_uthmani = unique_word.text_uthmani
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

    private const string PassVerdict = "pass";
    private const string FailVerdict = "fail";
    private const string HardSeverity = "hard";
    private const string WarningSeverity = "warning";

    private static readonly string DerivedUniqueCountsNote =
        "Unique counts are derived from the database; informational expectations are "
        + $"{DisplayWordsInvariants.InformationalUniqueTashkeel.ToString("N0", CultureInfo.InvariantCulture)} / "
        + $"{DisplayWordsInvariants.InformationalUniqueSimple.ToString("N0", CultureInfo.InvariantCulture)}.";

    private const string RollbackTotalsNote =
        "Totals reflect the attempted in-transaction rebuild before rollback; no derived rows were persisted.";

    private readonly QuranDashboardDbContext dbContext;
    private readonly ILinkingDataRevisionWriterStore revisionStore;

    public SqlDisplayWordsRebuilder(
        QuranDashboardDbContext dbContext,
        ILinkingDataRevisionWriterStore? revisionStore = null)
    {
        this.dbContext = dbContext;
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
            await revisionStore.LockForWriteAsync(npgsqlConnection, transaction, ct);

            if (!force && await AnyTargetTableHasDataAsync(npgsqlConnection, transaction, ct))
            {
                throw new InvalidOperationException(DisplayWordsInvariants.TargetsNotEmpty);
            }

            var sourceCountsBefore = await ReadSourceCountsAsync(npgsqlConnection, transaction, ct);
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

            var totals = await GatherTotalsAsync(npgsqlConnection, transaction, ct);
            var checks = await RunHardChecksAsync(
                npgsqlConnection,
                transaction,
                expectedReadableWords,
                sourceCountsBefore,
                totals,
                ct);

            var hardChecks = checks.Where(check => check.Severity == HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);
            var warnings = BuildWarningMessages(checks);

            if (allHardPassed)
            {
                await revisionStore.IncrementAsync(npgsqlConnection, transaction, ct);
                await transaction.CommitAsync(ct);

                return new DisplayWordsRebuildResult(
                    runAtUtc,
                    PassVerdict,
                    Persisted: true,
                    Forced: force,
                    totals,
                    checks,
                    warnings,
                    Errors: [],
                    BuildInfoNotes(persisted: true));
            }

            await transaction.RollbackAsync(ct);

            var errors = hardChecks
                .Where(check => !check.Passed)
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList();

            return new DisplayWordsRebuildResult(
                runAtUtc,
                FailVerdict,
                Persisted: false,
                Forced: force,
                totals,
                checks,
                warnings,
                errors,
                BuildInfoNotes(persisted: false));
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<List<DisplayWordsCheckResult>> RunHardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        SourceCounts sourceCountsBefore,
        DisplayWordsTotals totals,
        CancellationToken ct)
    {
        var expectedText = FormatInt(expectedReadableWords);
        var checks = new List<DisplayWordsCheckResult>();

        var orderedTashkeelCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdCountOrderedTashkeel, ct);
        var orderedSimpleCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdCountOrderedSimple, ct);
        var ordCountPassed = orderedTashkeelCount == expectedReadableWords
            && orderedSimpleCount == expectedReadableWords;
        checks.Add(new DisplayWordsCheckResult(
            "ORD-COUNT",
            HardSeverity,
            expectedText,
            $"tashkeel={FormatInt(orderedTashkeelCount)}, simple={FormatInt(orderedSimpleCount)}",
            ordCountPassed));

        var readableCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdReadable, ct);
        checks.Add(new DisplayWordsCheckResult(
            "ORD-READABLE",
            HardSeverity,
            expectedText,
            FormatInt(readableCount),
            readableCount == expectedReadableWords));

        var markerMappedCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdNoMarkers, ct);
        checks.Add(new DisplayWordsCheckResult(
            "ORD-NO-MARKERS",
            HardSeverity,
            "0",
            FormatInt(markerMappedCount),
            markerMappedCount == 0));

        var bijectionViolations = await ExecuteScalarIntAsync(
            connection,
            transaction,
            DisplayWordsSql.CheckOrdBijectionViolations,
            ct,
            new NpgsqlParameter("expected", expectedReadableWords));
        checks.Add(new DisplayWordsCheckResult(
            "ORD-BIJECTION",
            HardSeverity,
            expectedText,
            bijectionViolations == 0 ? expectedText : $"{FormatInt(bijectionViolations)} violation(s)",
            bijectionViolations == 0));

        var mushafViolations = await ExecuteScalarIntAsync(
            connection,
            transaction,
            DisplayWordsSql.CheckOrdMushafContigViolations,
            ct,
            new NpgsqlParameter("expected", expectedReadableWords));
        checks.Add(new DisplayWordsCheckResult(
            "ORD-MUSHAF-CONTIG",
            HardSeverity,
            $"min=1,max={expectedText},distinct={expectedText}",
            mushafViolations == 0 ? "contiguous" : $"{FormatInt(mushafViolations)} violation(s)",
            mushafViolations == 0));

        var surahViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdSurahContigViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "ORD-SURAH-CONTIG",
            HardSeverity,
            "0 violating surahs",
            surahViolations == 0 ? "0" : FormatInt(surahViolations),
            surahViolations == 0));

        var ayahViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdAyahContigViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "ORD-AYAH-CONTIG",
            HardSeverity,
            "0 violating ayahs",
            ayahViolations == 0 ? "0" : FormatInt(ayahViolations),
            ayahViolations == 0));

        var uniqueTashkeelCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountUniqueTashkeel, ct);
        var distinctTashkeelText = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountDistinctTashkeelText, ct);
        var uniqueSimpleCount = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountUniqueSimple, ct);
        var distinctSimpleText = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountDistinctSimpleText, ct);
        var unqCountPassed = uniqueTashkeelCount == distinctTashkeelText
            && uniqueSimpleCount == distinctSimpleText;
        checks.Add(new DisplayWordsCheckResult(
            "UNQ-COUNT",
            HardSeverity,
            $"tashkeel={FormatInt(distinctTashkeelText)}, simple={FormatInt(distinctSimpleText)}",
            $"tashkeel={FormatInt(uniqueTashkeelCount)}, simple={FormatInt(uniqueSimpleCount)}",
            unqCountPassed));

        var unqIdDeterministicViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqIdDeterministicViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "UNQ-ID-DETERMINISTIC",
            HardSeverity,
            "id = first_quran_word_id",
            unqIdDeterministicViolations == 0 ? "0 violations" : $"{FormatInt(unqIdDeterministicViolations)} violation(s)",
            unqIdDeterministicViolations == 0));

        var unqIdDuplicateViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqIdDuplicateViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "UNQ-ID-UNIQUE",
            HardSeverity,
            "0 duplicate ids",
            unqIdDuplicateViolations == 0 ? "0" : FormatInt(unqIdDuplicateViolations),
            unqIdDuplicateViolations == 0));

        var statViolations = await ExecuteScalarIntAsync(
            connection,
            transaction,
            DisplayWordsSql.CheckStatMatchViolations,
            ct,
            new NpgsqlParameter("expected", expectedReadableWords));
        checks.Add(new DisplayWordsCheckResult(
            "STAT-MATCH",
            HardSeverity,
            $"sum occurrences={expectedText}",
            statViolations == 0 ? expectedText : $"{FormatInt(statViolations)} violation(s)",
            statViolations == 0));

        var firstOccViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckFirstOccViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "FIRST-OCC",
            HardSeverity,
            "first_* matches earliest ordered row",
            firstOccViolations == 0 ? "0 violations" : $"{FormatInt(firstOccViolations)} violation(s)",
            firstOccViolations == 0));

        var linkReadableViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckLinkReadableCompleteViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "LINK-READABLE-COMPLETE",
            HardSeverity,
            "0",
            FormatInt(linkReadableViolations),
            linkReadableViolations == 0));

        var linkMarkersViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckLinkMarkersNullViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "LINK-MARKERS-NULL",
            HardSeverity,
            "0",
            FormatInt(linkMarkersViolations),
            linkMarkersViolations == 0));

        var linkResolvesViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckLinkResolvesViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "LINK-RESOLVES",
            HardSeverity,
            "0",
            FormatInt(linkResolvesViolations),
            linkResolvesViolations == 0));

        var linkConsistentViolations = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckLinkConsistentViolations, ct);
        checks.Add(new DisplayWordsCheckResult(
            "LINK-CONSISTENT",
            HardSeverity,
            "0",
            FormatInt(linkConsistentViolations),
            linkConsistentViolations == 0));

        var sourceCountsAfter = await ReadSourceCountsAsync(connection, transaction, ct);
        var sourceUntouched = sourceCountsBefore == sourceCountsAfter;
        checks.Add(new DisplayWordsCheckResult(
            "SRC-UNTOUCHED",
            HardSeverity,
            FormatSourceCounts(sourceCountsBefore),
            FormatSourceCounts(sourceCountsAfter),
            sourceUntouched));

        var tashkeelWarningPassed = totals.UniqueTashkeelRows == DisplayWordsInvariants.InformationalUniqueTashkeel;
        checks.Add(new DisplayWordsCheckResult(
            "UNQ-EXPECT-TASHKEEL",
            WarningSeverity,
            FormatInt(DisplayWordsInvariants.InformationalUniqueTashkeel),
            FormatInt(totals.UniqueTashkeelRows),
            tashkeelWarningPassed));

        var simpleWarningPassed = totals.UniqueSimpleRows == DisplayWordsInvariants.InformationalUniqueSimple;
        checks.Add(new DisplayWordsCheckResult(
            "UNQ-EXPECT-SIMPLE",
            WarningSeverity,
            FormatInt(DisplayWordsInvariants.InformationalUniqueSimple),
            FormatInt(totals.UniqueSimpleRows),
            simpleWarningPassed));

        return checks;
    }

    private static IReadOnlyList<string> BuildInfoNotes(bool persisted)
    {
        return persisted
            ? [DerivedUniqueCountsNote]
            : [DerivedUniqueCountsNote, RollbackTotalsNote];
    }

    private static string FormatInt(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string FormatSourceCounts(SourceCounts counts) =>
        $"words={FormatInt(counts.Words)}, ayahs={FormatInt(counts.Ayahs)}, surahs={FormatInt(counts.Surahs)}";

    private static List<string> BuildWarningMessages(IReadOnlyList<DisplayWordsCheckResult> checks)
    {
        return checks
            .Where(check => check.Severity == WarningSeverity && !check.Passed)
            .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
            .ToList();
    }

    private static async Task<DisplayWordsTotals> GatherTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var orderedTashkeelRows = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdCountOrderedTashkeel, ct);
        var orderedSimpleRows = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdCountOrderedSimple, ct);
        var uniqueTashkeelRows = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountUniqueTashkeel, ct);
        var uniqueSimpleRows = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckUnqCountUniqueSimple, ct);
        var readableWords = await ExecuteScalarIntAsync(
            connection, transaction, DisplayWordsSql.CheckOrdReadable, ct);

        return new DisplayWordsTotals(
            orderedTashkeelRows,
            orderedSimpleRows,
            uniqueTashkeelRows,
            uniqueSimpleRows,
            readableWords);
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

    private static async Task<SourceCounts> ReadSourceCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        var words = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceWordsCount, ct);
        var ayahs = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceAyahsCount, ct);
        var surahs = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceSurahsCount, ct);
        return new SourceCounts(words, ayahs, surahs);
    }

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken ct,
        params NpgsqlParameter[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private sealed record SourceCounts(int Words, int Ayahs, int Surahs);

}
