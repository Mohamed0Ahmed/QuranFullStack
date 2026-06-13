using System.Globalization;
using System.Text.Json;
using Npgsql;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Mutashabihat;

internal static class MutashabihatImportReportBuilder
{
    private const string WarningSeverity = MutashabihatImportConstants.WarningSeverity;
    private const string InfoSeverity = MutashabihatImportConstants.InfoSeverity;

    public static List<MutashabihatCheckResult> BuildAssemblyWarningChecks(
        MutashabihatSourceData source,
        int rawOccurrenceCount,
        int provenanceLicenseUnknownCount)
    {
        var storedOccurrenceCount = source.Groups.Sum(group => group.Occurrences.Count);
        var duplicateCount = rawOccurrenceCount - storedOccurrenceCount;
        var sourceKeyAbsentCount = source.Groups.Count(
            group => !group.Occurrences.Any(occurrence => occurrence.AyahId == group.RepresentativeAyahId));
        var staleCounterCount = CountStaleSourceCounters(source);

        return
        [
            BuildCountCheck(
                MutashabihatInvariants.CheckDuplicateOccurrence,
                WarningSeverity,
                MutashabihatInvariants.ExpectedDuplicateOccurrence,
                duplicateCount),
            BuildCountCheck(
                MutashabihatInvariants.CheckSourceKeyAbsent,
                WarningSeverity,
                MutashabihatInvariants.ExpectedSourceKeyAbsent,
                sourceKeyAbsentCount),
            BuildCountCheck(
                MutashabihatInvariants.CheckStaleSourceCounters,
                WarningSeverity,
                "recomputed values win",
                staleCounterCount),
            BuildCountCheck(
                MutashabihatInvariants.CheckProvenanceLicenseUnknown,
                WarningSeverity,
                "2 source files",
                provenanceLicenseUnknownCount)
        ];
    }

    public static async Task<List<MutashabihatCheckResult>> RunPostCopyWarningAndInfoChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var checks = new List<MutashabihatCheckResult>();

        var coverageGt100 = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckCoverageGt100, ct);
        checks.Add(BuildCountCheck(
            MutashabihatInvariants.CheckCoverageGt100,
            WarningSeverity,
            MutashabihatInvariants.ExpectedCoverageGt100,
            coverageGt100));

        var occurrenceUpperBound = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOccurrenceWordRangeUpperBound, ct);
        var linkUpperBound = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckLinkMatchWordsUpperBound, ct);
        var wordRangeUpperBound = occurrenceUpperBound + linkUpperBound;
        checks.Add(new MutashabihatCheckResult(
            MutashabihatInvariants.CheckWordRangeUpperBound,
            WarningSeverity,
            "count",
            FormatInt(wordRangeUpperBound),
            true));

        var onewayLinks = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOnewayLinks, ct);
        checks.Add(new MutashabihatCheckResult(
            MutashabihatInvariants.CheckOnewayLinks,
            InfoSeverity,
            "≈1120",
            FormatApprox(onewayLinks),
            true));

        var overlapAyahs = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckCrossDatasetOverlapAyahs, ct);
        var overlapPairs = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckCrossDatasetOverlapPairs, ct);
        checks.Add(new MutashabihatCheckResult(
            MutashabihatInvariants.CheckCrossDatasetOverlap,
            InfoSeverity,
            "≈792 ayahs / 813 pairs",
            $"{FormatApprox(overlapAyahs)} ayahs / {FormatApprox(overlapPairs)} pairs",
            true));

        var distinctSurahs = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckDistinctReferencedSurahs, ct);
        var distinctAyahs = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckDistinctReferencedAyahs, ct);
        checks.Add(new MutashabihatCheckResult(
            MutashabihatInvariants.CheckSurahCoverage,
            InfoSeverity,
            "109/114 surahs; 3084 distinct ayahs",
            $"{FormatInt(distinctSurahs)}/114 surahs; {FormatInt(distinctAyahs)} distinct ayahs",
            true));

        return checks;
    }

    public static List<string> BuildWarnings(IReadOnlyList<MutashabihatCheckResult> checks)
    {
        var warnings = new List<string>();

        foreach (var check in checks.Where(check => check.Severity == WarningSeverity))
        {
            warnings.Add(FormatWarningMessage(check));
        }

        return warnings;
    }

    public static List<string> BuildInfoNotes(
        IReadOnlyList<MutashabihatCheckResult> checks,
        IReadOnlyList<string> existingInfoNotes)
    {
        var infoNotes = new List<string>(existingInfoNotes);

        foreach (var check in checks.Where(check => check.Severity == InfoSeverity))
        {
            infoNotes.Add($"{check.Id}: {check.Observed}.");
        }

        return infoNotes;
    }

    private static int CountStaleSourceCounters(MutashabihatSourceData source)
    {
        var staleCount = 0;

        foreach (var group in source.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.RawSourceCountsJson))
            {
                continue;
            }

            using var document = JsonDocument.Parse(group.RawSourceCountsJson);
            var root = document.RootElement;

            if (root.TryGetProperty("count", out var count) &&
                count.ValueKind == JsonValueKind.Number &&
                count.GetInt32() != group.OccurrenceCount)
            {
                staleCount++;
                continue;
            }

            if (root.TryGetProperty("ayahs", out var ayahs) &&
                ayahs.ValueKind == JsonValueKind.Number &&
                ayahs.GetInt32() != group.DistinctAyahCount)
            {
                staleCount++;
                continue;
            }

            if (root.TryGetProperty("surahs", out var surahs) &&
                surahs.ValueKind == JsonValueKind.Number &&
                surahs.GetInt32() != group.DistinctSurahCount)
            {
                staleCount++;
            }
        }

        return staleCount;
    }

    private static MutashabihatCheckResult BuildCountCheck(
        string id,
        string severity,
        object expected,
        int observed) =>
        new(
            id,
            severity,
            expected.ToString() ?? string.Empty,
            FormatInt(observed),
            true);

    private static string FormatWarningMessage(MutashabihatCheckResult check) =>
        check.Id switch
        {
            MutashabihatInvariants.CheckCoverageGt100 =>
                $"{check.Id}: {check.Observed} links with coverage > 100 stored raw.",
            MutashabihatInvariants.CheckDuplicateOccurrence =>
                $"{check.Id}: {check.Observed} identical occurrence collapsed.",
            MutashabihatInvariants.CheckSourceKeyAbsent =>
                $"{check.Id}: {check.Observed} group(s) whose source.key is absent from occurrences.",
            MutashabihatInvariants.CheckStaleSourceCounters =>
                $"{check.Id}: {check.Observed} group(s) with stale source counters; recomputed values win.",
            MutashabihatInvariants.CheckWordRangeUpperBound =>
                $"{check.Id}: {check.Observed} word range(s) exceed ayah words_count_real; stored unchanged.",
            MutashabihatInvariants.CheckProvenanceLicenseUnknown =>
                $"{check.Id}: provenance/license unknown for {check.Observed} source file(s) (blocks future publishing).",
            _ => $"{check.Id}: expected {check.Expected}, observed {check.Observed}."
        };

    private static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatApprox(int value) => $"~{FormatInt(value)}";
}
