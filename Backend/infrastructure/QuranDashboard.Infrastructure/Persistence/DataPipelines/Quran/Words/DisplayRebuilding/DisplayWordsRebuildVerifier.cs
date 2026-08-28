using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.DisplayRebuilding;

internal sealed class DisplayWordsRebuildVerifier
{
    private const int CommandTimeoutSeconds = 600;
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

    public async Task<DisplayWordsSourceCounts> ReadSourceCountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var words = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceWordsCount, ct);
        var ayahs = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceAyahsCount, ct);
        var surahs = await ExecuteScalarIntAsync(connection, transaction, DisplayWordsSql.CheckSourceSurahsCount, ct);
        return new DisplayWordsSourceCounts(words, ayahs, surahs);
    }

    public async Task<DisplayWordsRebuildAssessment> VerifyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        DisplayWordsSourceCounts sourceCountsBefore,
        CancellationToken ct)
    {
        var totals = await GatherTotalsAsync(connection, transaction, ct);
        var checks = await RunChecksAsync(
            connection,
            transaction,
            expectedReadableWords,
            sourceCountsBefore,
            totals,
            ct);
        var hardErrors = checks
            .Where(check => check.Severity == HardSeverity && !check.Passed)
            .Select(FormatFailure)
            .ToList();
        var warnings = checks
            .Where(check => check.Severity == WarningSeverity && !check.Passed)
            .Select(FormatFailure)
            .ToList();

        return new DisplayWordsRebuildAssessment(
            totals,
            checks,
            warnings,
            hardErrors,
            AllHardChecksPassed: hardErrors.Count == 0);
    }

    public DisplayWordsRebuildResult BuildResult(
        DateTimeOffset runAtUtc,
        bool force,
        DisplayWordsRebuildAssessment assessment,
        bool persisted,
        IReadOnlyList<string> errors)
    {
        return new DisplayWordsRebuildResult(
            runAtUtc,
            persisted ? PassVerdict : FailVerdict,
            Persisted: persisted,
            Forced: force,
            assessment.Totals,
            assessment.Checks,
            assessment.Warnings,
            errors,
            BuildInfoNotes(persisted));
    }

    private async Task<List<DisplayWordsCheckResult>> RunChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        DisplayWordsSourceCounts sourceCountsBefore,
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
            connection, transaction, DisplayWordsFirstOccurrenceValidationSql.CheckViolations, ct);
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

    private static IReadOnlyList<string> BuildInfoNotes(bool persisted)
    {
        return persisted
            ? [DerivedUniqueCountsNote]
            : [DerivedUniqueCountsNote, RollbackTotalsNote];
    }

    private static string FormatFailure(DisplayWordsCheckResult check) =>
        $"{check.Id}: expected {check.Expected}, observed {check.Observed}";

    private static string FormatInt(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string FormatSourceCounts(DisplayWordsSourceCounts counts) =>
        $"words={FormatInt(counts.Words)}, ayahs={FormatInt(counts.Ayahs)}, surahs={FormatInt(counts.Surahs)}";

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
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
}

internal sealed record DisplayWordsSourceCounts(int Words, int Ayahs, int Surahs);

internal sealed record DisplayWordsRebuildAssessment(
    DisplayWordsTotals Totals,
    IReadOnlyList<DisplayWordsCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> HardErrors,
    bool AllHardChecksPassed);
