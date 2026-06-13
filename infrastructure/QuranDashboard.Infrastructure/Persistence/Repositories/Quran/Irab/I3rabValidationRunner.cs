using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Files.Quran.Morphology.Irab;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

internal static class I3rabValidationRunner
{
    private const string HardSeverity = "hard";

    public static async Task<IReadOnlyList<I3rabCheckResult>> RunHardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        I3rabSourceSnapshot before,
        I3rabSourceSnapshot after,
        I3rabExpectedCounts expectedCounts,
        CancellationToken ct)
    {
        var expectedSegments = FormatInt(expectedCounts.SegmentCount);
        var expectedWords = FormatInt(expectedCounts.ReadableWordCount);
        var expectedNullForms = FormatInt(expectedCounts.NullFormCount);

        var completeStatusCount = await ExecuteScalarIntAsync(
            connection, transaction, I3rabSql.CountCompleteStatuses, ct);
        var displayableWordCount = await ExecuteScalarIntAsync(
            connection, transaction, I3rabSql.CountDisplayableWords, ct);
        var undisplayableWordCount = await ExecuteScalarIntAsync(
            connection, transaction, I3rabSql.CountUndisplayableWords, ct);

        var checks = new List<I3rabCheckResult>
        {
            new(
                I3rabInvariants.CheckSegStatusComplete,
                HardSeverity,
                expectedSegments,
                FormatInt(completeStatusCount),
                completeStatusCount == expectedCounts.SegmentCount),
            await BuildCountCheckAsync(
                connection,
                transaction,
                I3rabInvariants.CheckApprovedConsistent,
                "0",
                I3rabSql.CountApprovedViolations,
                value => value == 0,
                ct),
            await BuildCountCheckAsync(
                connection,
                transaction,
                I3rabInvariants.CheckNeedsReviewConsistent,
                "0",
                I3rabSql.CountNeedsReviewViolations,
                value => value == 0,
                ct),
            await BuildCountCheckAsync(
                connection,
                transaction,
                I3rabInvariants.CheckUnsupportedConsistent,
                "0",
                I3rabSql.CountUnsupportedViolations,
                value => value == 0,
                ct),
            new(
                I3rabInvariants.CheckWordDisplayable,
                HardSeverity,
                expectedWords,
                FormatInt(displayableWordCount),
                undisplayableWordCount == 0
                    && displayableWordCount == expectedCounts.ReadableWordCount),
            await BuildCountCheckAsync(
                connection,
                transaction,
                I3rabInvariants.CheckRuleResolves,
                "0",
                I3rabSql.CountDanglingRuleIds,
                value => value == 0,
                ct),
            new(
                I3rabInvariants.CheckSourceColumnsUnchanged,
                HardSeverity,
                "unchanged",
                BuildSourceObserved(before, after),
                before.SegmentFingerprint == after.SegmentFingerprint
                    && before.QuranWordsFingerprint == after.QuranWordsFingerprint
                    && before.PosTagsFingerprint == after.PosTagsFingerprint),
            new(
                I3rabInvariants.CheckSegmentRowCountStable,
                HardSeverity,
                expectedSegments,
                FormatInt(after.SegmentCount),
                before.SegmentCount == expectedCounts.SegmentCount
                    && after.SegmentCount == expectedCounts.SegmentCount),
            new(
                I3rabInvariants.CheckNullFormNotInvented,
                HardSeverity,
                expectedNullForms,
                FormatInt(after.NullFormSegmentIds.Count),
                before.NullFormSegmentIds.Count == expectedCounts.NullFormCount
                    && after.NullFormSegmentIds.Count == expectedCounts.NullFormCount
                    && before.NullFormSegmentIds.SequenceEqual(after.NullFormSegmentIds))
        };

        return checks;
    }

    public static async Task<IReadOnlyList<I3rabWarning>> BuildWarningsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        int segmentCount,
        int approvedCount,
        int needsReviewCount,
        int unsupportedCount,
        CancellationToken ct)
    {
        var warnings = new List<I3rabWarning>
        {
            new(
                I3rabInvariants.WarningCoverage,
                $"approved={approvedCount}/{segmentCount}, needs_review={needsReviewCount}, unsupported={unsupportedCount}"),
            new(
                I3rabInvariants.WarningUnknownPatterns,
                unsupportedCount == 0 ? "none" : $"{unsupportedCount} unsupported segment(s)"),
            new(
                I3rabInvariants.WarningNeedsReviewSummary,
                needsReviewCount == 0 ? "none" : $"{needsReviewCount} needs-review segment(s)")
        };

        var ruleUsage = await ReadRuleUsageAsync(connection, transaction, ct);
        var unusedRules = ruleUsage.Count(row => row.SegmentCount == 0);
        var familyCount = ruleUsage.Select(row => row.RuleFamily).Distinct(StringComparer.Ordinal).Count();
        warnings.Add(new(
            I3rabInvariants.WarningRuleUsage,
            $"{ruleUsage.Count} rules across {familyCount} families tracked; {unusedRules} rules unused"));

        var labelCorrectionCount = await CountLabelCorrectionsAsync(connection, transaction, ct);
        warnings.Add(new(
            I3rabInvariants.WarningLabelReview,
            labelCorrectionCount == 0
                ? "no rule-layer label corrections present"
                : $"{labelCorrectionCount} rule-layer label corrections present (catalogue owns labels; overrides quran_pos_tags seed)"));

        return warnings;
    }

    public static async Task<IReadOnlyList<I3rabRuleUsageRow>> ReadRuleUsageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(I3rabSql.RuleUsage, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var rows = new List<I3rabRuleUsageRow>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new I3rabRuleUsageRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return rows;
    }

    public static async Task<I3rabSourceSnapshot> CaptureSnapshotAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        var segmentCount = await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountSegments, ct);
        var readableWordCount = await ExecuteScalarIntAsync(connection, transaction, I3rabSql.CountReadableWords, ct);
        var segmentFingerprint = await ExecuteScalarStringAsync(connection, transaction, I3rabSql.SegmentSourceFingerprint, ct);
        var wordsFingerprint = await ExecuteScalarStringAsync(connection, transaction, I3rabSql.QuranWordsFingerprint, ct);
        var posTagsFingerprint = await ExecuteScalarStringAsync(connection, transaction, I3rabSql.PosTagsFingerprint, ct);
        var nullFormIds = await ReadNullFormSegmentIdsAsync(connection, transaction, ct);

        return new I3rabSourceSnapshot(
            segmentCount,
            readableWordCount,
            segmentFingerprint,
            wordsFingerprint,
            posTagsFingerprint,
            nullFormIds);
    }

    private static async Task<I3rabCheckResult> BuildCountCheckAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string id,
        string expected,
        string sql,
        Func<int, bool> passed,
        CancellationToken ct)
    {
        var observedValue = await ExecuteScalarIntAsync(connection, transaction, sql, ct);
        return new I3rabCheckResult(id, HardSeverity, expected, FormatInt(observedValue), passed(observedValue));
    }

    private static string BuildSourceObserved(I3rabSourceSnapshot before, I3rabSourceSnapshot after)
    {
        var segmentChanged = before.SegmentFingerprint != after.SegmentFingerprint;
        var wordsChanged = before.QuranWordsFingerprint != after.QuranWordsFingerprint;
        var posChanged = before.PosTagsFingerprint != after.PosTagsFingerprint;

        if (!segmentChanged && !wordsChanged && !posChanged)
        {
            return "unchanged";
        }

        return $"segments={(segmentChanged ? "changed" : "unchanged")}, words={(wordsChanged ? "changed" : "unchanged")}, pos_tags={(posChanged ? "changed" : "unchanged")}";
    }

    private static async Task<IReadOnlyList<int>> ReadNullFormSegmentIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(I3rabSql.NullFormSegmentIds, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var ids = new List<int>();
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountLabelCorrectionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(I3rabSql.CountLabelCorrectionsPresent, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("correctedSignatures", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = I3rabSeedLabelCorrections.Signatures.ToArray()
        });
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<string> ExecuteScalarStringAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? string.Empty : Convert.ToString(result, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
}

internal sealed record I3rabRuleUsageRow(
    string SignatureKey,
    string RuleFamily,
    string I3rabArabic,
    int SegmentCount);
