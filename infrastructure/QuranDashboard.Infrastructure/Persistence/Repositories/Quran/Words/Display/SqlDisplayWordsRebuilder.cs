using System.Data;
using System.Globalization;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Words.Display;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Words.Display;

public sealed class SqlDisplayWordsRebuilder : IDisplayWordsRebuilder
{
    private const string PassVerdict = "pass";
    private const string FailVerdict = "fail";
    private const string HardSeverity = "hard";
    private const string WarningSeverity = "warning";

    private const string DerivedUniqueCountsNote =
        "Unique counts are derived from the database; informational expectations are 21,210 / 14,783.";

    private const string RollbackTotalsNote =
        "Totals reflect the attempted in-transaction rebuild before rollback; no derived rows were persisted.";

    private readonly QuranDashboardDbContext dbContext;

    public SqlDisplayWordsRebuilder(QuranDashboardDbContext dbContext)
    {
        this.dbContext = dbContext;
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
        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(DisplayWordsInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for display-words rebuild.");
        }

        var sourceCountsBefore = await ReadSourceCountsAsync(npgsqlConnection, null, ct);

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);
        await dbContext.Database.UseTransactionAsync(transaction, ct);

        try
        {
            if (force)
            {
                await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.TruncateDerivedTables, ct);
            }

            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertOrderedTashkeel, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertOrderedSimple, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertUniqueTashkeel, ct);
            await dbContext.Database.ExecuteSqlRawAsync(DisplayWordsSql.InsertUniqueSimple, ct);

            var totals = await GatherTotalsAsync(ct);
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

    private async Task<DisplayWordsTotals> GatherTotalsAsync(CancellationToken ct)
    {
        var orderedTashkeelRows = await dbContext.QuranWordsOrderedTashkeel.CountAsync(ct);
        var orderedSimpleRows = await dbContext.QuranWordsOrderedSimple.CountAsync(ct);
        var uniqueTashkeelRows = await dbContext.QuranWordsUniqueTashkeel.CountAsync(ct);
        var uniqueSimpleRows = await dbContext.QuranWordsUniqueSimple.CountAsync(ct);

        var readableWords = await dbContext.QuranWords.CountAsync(word => !word.IsAyahMarker, ct);

        return new DisplayWordsTotals(
            orderedTashkeelRows,
            orderedSimpleRows,
            uniqueTashkeelRows,
            uniqueSimpleRows,
            readableWords);
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
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private sealed record SourceCounts(int Words, int Ayahs, int Surahs);

}
