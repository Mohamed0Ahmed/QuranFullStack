using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Mutashabihat;

internal static class MutashabihatValidationRunner
{
    private const string HardSeverity = MutashabihatImportConstants.HardSeverity;

    public static List<MutashabihatCheckResult> BuildLoadTimeChecks(
        int rawOccurrenceCount,
        MutashabihatExpectedCounts expected)
    {
        return
        [
            new MutashabihatCheckResult(
                MutashabihatInvariants.CheckManifestSet,
                HardSeverity,
                "staged file set matches manifest",
                "exact file set",
                true),
            new MutashabihatCheckResult(
                MutashabihatInvariants.CheckManifestChecksum,
                HardSeverity,
                "each source file sha256 + size match manifest",
                "checksums match",
                true),
            new MutashabihatCheckResult(
                MutashabihatInvariants.CheckJsonShape,
                HardSeverity,
                "both JSON roots are objects with required fields",
                "shape valid",
                true),
            new MutashabihatCheckResult(
                "MUT-RAW-OCCURRENCE-COUNT",
                HardSeverity,
                FormatInt(expected.RawOccurrences),
                FormatInt(rawOccurrenceCount),
                rawOccurrenceCount == expected.RawOccurrences)
        ];
    }

    public static List<MutashabihatCheckResult> BuildPreCopyHardChecks(MutashabihatSourceData source)
    {
        var selfLinkCount = source.Links.Count(link => link.SourceAyahId == link.TargetAyahId);

        return
        [
            new MutashabihatCheckResult(
                MutashabihatInvariants.CheckLinkNoSelf,
                HardSeverity,
                "0 self-links",
                selfLinkCount == 0 ? "0 self-links" : $"{FormatInt(selfLinkCount)} self-link(s)",
                selfLinkCount == 0)
        ];
    }

    public static async Task<List<MutashabihatCheckResult>> RunPostCopyHardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MutashabihatExpectedCounts expected,
        CancellationToken ct)
    {
        var checks = new List<MutashabihatCheckResult>();

        var groupCount = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckGroupCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-GROUP-COUNT",
            HardSeverity,
            FormatInt(expected.Groups),
            FormatInt(groupCount),
            groupCount == expected.Groups));

        var storedOccurrenceCount = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckStoredOccurrenceCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-STORED-OCCURRENCE-COUNT",
            HardSeverity,
            FormatInt(expected.StoredOccurrences),
            FormatInt(storedOccurrenceCount),
            storedOccurrenceCount == expected.StoredOccurrences));

        var similarSourceCount = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckSimilarSourceCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-SIMILAR-SOURCE-COUNT",
            HardSeverity,
            FormatInt(expected.SimilarSources),
            FormatInt(similarSourceCount),
            similarSourceCount == expected.SimilarSources));

        var similarLinkCount = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckSimilarLinkCount, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-SIMILAR-LINK-COUNT",
            HardSeverity,
            FormatInt(expected.SimilarLinks),
            FormatInt(similarLinkCount),
            similarLinkCount == expected.SimilarLinks));

        checks.Add(new MutashabihatCheckResult(
            "MUT-VERSEKEY-FORMAT",
            HardSeverity,
            "every reference matches ^\\d+:\\d+$",
            "validated during assembly",
            true));

        var duplicateViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOccurrenceUniqueViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-OCCURRENCE-UNIQUE",
            HardSeverity,
            "0 duplicate tuples",
            duplicateViolations == 0 ? "0 duplicates" : $"{FormatInt(duplicateViolations)} duplicate tuple(s)",
            duplicateViolations == 0));

        var minSizeViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckGroupMinSizeViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-GROUP-MIN-SIZE",
            HardSeverity,
            "every group distinct_ayah_count >= 2",
            minSizeViolations == 0 ? "0 violations" : $"{FormatInt(minSizeViolations)} violation(s)",
            minSizeViolations == 0));

        var occurrenceWordRangeViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckOccurrenceWordRangeViolations, ct);
        var linkWordRangeViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckLinkWordRangeViolations, ct);
        var wordRangeViolations = occurrenceWordRangeViolations + linkWordRangeViolations;
        checks.Add(new MutashabihatCheckResult(
            "MUT-WORD-RANGE-SHAPE",
            HardSeverity,
            "occurrence and match_words ranges have from >= 1 and to >= from",
            wordRangeViolations == 0 ? "0 violations" : $"{FormatInt(wordRangeViolations)} violation(s)",
            wordRangeViolations == 0));

        var selfLinkViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckSelfLinkViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            MutashabihatInvariants.CheckLinkNoSelf,
            HardSeverity,
            "0 self-links",
            selfLinkViolations == 0 ? "0 self-links" : $"{FormatInt(selfLinkViolations)} self-link(s)",
            selfLinkViolations == 0));

        var scoreRangeViolations = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckScoreRangeViolations, ct);
        checks.Add(new MutashabihatCheckResult(
            "MUT-SCORE-RANGE",
            HardSeverity,
            "every link score in [50, 100]",
            scoreRangeViolations == 0 ? "0 violations" : $"{FormatInt(scoreRangeViolations)} violation(s)",
            scoreRangeViolations == 0));

        var unresolvedRepresentatives = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedGroupRepresentativeAyahs, ct);
        var unresolvedOccurrences = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedOccurrenceAyahs, ct);
        var unresolvedLinkSources = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedLinkSourceAyahs, ct);
        var unresolvedLinkTargets = await MutashabihatCommandExecutor.ExecuteScalarIntAsync(
            connection, transaction, MutashabihatSql.CheckUnresolvedLinkTargetAyahs, ct);
        var unresolvedTotal = unresolvedRepresentatives
            + unresolvedOccurrences
            + unresolvedLinkSources
            + unresolvedLinkTargets;
        checks.Add(new MutashabihatCheckResult(
            "MUT-AYAH-RESOLVE",
            HardSeverity,
            "0 unresolved ayah references",
            unresolvedTotal == 0 ? "0 unresolved" : $"{FormatInt(unresolvedTotal)} unresolved",
            unresolvedTotal == 0));

        return checks;
    }

    private static string FormatInt(int value) => value.ToString(CultureInfo.InvariantCulture);
}
