using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

// Evaluates every hard validation gate over the freshly COPYed rows inside the import
// transaction and produces the MorphologyCheckResult list that decides commit vs. rollback.
internal static class MorphologyValidationRunner
{
    private const string HardSeverity = MorphologyImportConstants.HardSeverity;

    // MORPH-POS-RESOLVES is determined in memory from the source vs. the controlled vocabulary
    // (PosTagSeed), the same way MORPH-SEG-CHARSET is determined from CharsetWarnings. This is the
    // single source of truth and is evaluated before the COPY (see EfBulkMorphologyWriter.ImportAsync)
    // so an unknown code fails closed with a report rather than tripping the quran_pos_tags foreign key
    // mid-COPY.
    public static MorphologyCheckResult BuildPosResolvesCheck(MorphologySourceData source)
    {
        var unknownCount = source.UnknownPosCodes.Count;
        return new MorphologyCheckResult(
            "MORPH-POS-RESOLVES",
            HardSeverity,
            "every head_pos and segment pos resolves to quran_pos_tags.code (0 unknown)",
            unknownCount == 0
                ? "0 unknown"
                : $"{FormatInt(unknownCount)} unknown ({string.Join(", ", source.UnknownPosCodes)})",
            unknownCount == 0);
    }

    public static async Task<List<MorphologyCheckResult>> RunAllHardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        MorphologySourceData source,
        SegmentArabicRenderer renderer,
        CancellationToken ct)
    {
        var checks = new List<MorphologyCheckResult>();

        await AddUs1ChecksAsync(checks, connection, transaction, expectedReadableWords, ct);
        await AddUs3ChecksAsync(checks, connection, transaction, source, renderer, ct);

        return checks;
    }

    private static async Task AddUs1ChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        CancellationToken ct)
    {
        var expectedText = FormatInt(expectedReadableWords);

        var morphologyCount = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckReadableComplete, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-READABLE-COMPLETE",
            HardSeverity,
            expectedText,
            FormatInt(morphologyCount),
            morphologyCount == expectedReadableWords));

        var markerMorphologyCount = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckMarkersExcludedMorphology, ct);
        var markerSegmentCount = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckMarkersExcludedSegments, ct);
        var markerTotal = markerMorphologyCount + markerSegmentCount;
        checks.Add(new MorphologyCheckResult(
            "MORPH-MARKERS-EXCLUDED",
            HardSeverity,
            "0",
            FormatInt(markerTotal),
            markerTotal == 0));

        var locationIdMismatches = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckLocationIdMismatch, ct);
        var unmatchedReadable = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckLocationUnmatchedReadable, ct);
        var locationViolations = locationIdMismatches + unmatchedReadable;
        checks.Add(new MorphologyCheckResult(
            "MORPH-LOCATION-MATCH",
            HardSeverity,
            "0 id/location mismatches; 0 unmatched readable words",
            $"id_mismatch={FormatInt(locationIdMismatches)}, unmatched_readable={FormatInt(unmatchedReadable)}",
            locationViolations == 0));

        var segmentViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegmentsPresentViolations, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEGMENTS-PRESENT",
            HardSeverity,
            "segment_count matches segment rows",
            segmentViolations == 0 ? "0 violations" : $"{FormatInt(segmentViolations)} violation(s)",
            segmentViolations == 0));

        var nullPosCount = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckPosPresentNullSegmentPos, ct);
        var stemViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckPosPresentStemCountViolations, ct);
        var posViolations = nullPosCount + stemViolations;
        checks.Add(new MorphologyCheckResult(
            "MORPH-POS-PRESENT",
            HardSeverity,
            "at least one STEM per word; head_pos = first STEM pos by segment_number",
            posViolations == 0 ? "0 violations" : $"{FormatInt(posViolations)} violation(s)",
            posViolations == 0));

        var verbViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckVerbFeatureViolations, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-VERB-FEATURE-CONSISTENCY",
            HardSeverity,
            "head verbs: exactly one head-STEM tense + valid voice; non-verb heads: null word-level verb fields",
            verbViolations == 0 ? "0 violations" : $"{FormatInt(verbViolations)} violation(s)",
            verbViolations == 0));
    }

    private static async Task AddUs3ChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MorphologySourceData source,
        SegmentArabicRenderer renderer,
        CancellationToken ct)
    {
        var charsetViolations = source.CharsetWarnings.Count;
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-CHARSET",
            HardSeverity,
            "0 unmapped characters; space allowed only for multiword-tier forms",
            $"{FormatInt(charsetViolations)} unmapped",
            charsetViolations == 0));

        var nonEmptyNullRender = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegRenderTotalNonEmpty, ct);
        var emptyNonNullRender = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegRenderTotalEmpty, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-RENDER-TOTAL",
            HardSeverity,
            "non-empty form → non-null render; empty form → NULL",
            $"non_empty_null={FormatInt(nonEmptyNullRender)}, empty_non_null={FormatInt(emptyNonNullRender)}",
            nonEmptyNullRender == 0 && emptyNonNullRender == 0));

        var tierViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegTierValid, ct);
        var sourceViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegSourceValid, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-TIER-VALID",
            HardSeverity,
            "valid tier + correct source on all rendered rows",
            $"tier_violations={FormatInt(tierViolations)}, source_violations={FormatInt(sourceViolations)}",
            tierViolations == 0 && sourceViolations == 0));

        var provenance = await CheckRenderProvenanceAsync(connection, transaction, renderer, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-RENDER-PROVENANCE",
            HardSeverity,
            "rendered rows retain non-empty form_buckwalter; source = buckwalter-transliteration; Arabic and tier recompute from each row's own form_buckwalter; deterministic Uthmani/QPC equality is allowed",
            $"missing_buckwalter={FormatInt(provenance.MissingBuckwalter)}, " +
            $"source_violations={FormatInt(provenance.SourceViolations)}, " +
            $"render_mismatches={FormatInt(provenance.RenderMismatches)}, " +
            $"tier_mismatches={FormatInt(provenance.TierMismatches)}",
            provenance.Passed));

        var danglingRoots = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckDimensionResolvesRoots, ct);
        var danglingLemmas = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckDimensionResolvesLemmas, ct);
        var danglingStems = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckDimensionResolvesStems, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-DIMENSION-RESOLVES",
            HardSeverity,
            "0 dangling dimension references",
            $"roots={FormatInt(danglingRoots)}, lemmas={FormatInt(danglingLemmas)}, stems={FormatInt(danglingStems)}",
            danglingRoots == 0 && danglingLemmas == 0 && danglingStems == 0));
    }

    private static async Task<RenderProvenanceCounts> CheckRenderProvenanceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SegmentArabicRenderer renderer,
        CancellationToken ct)
    {
        var missingBuckwalter = 0;
        var sourceViolations = 0;
        var renderMismatches = 0;
        var tierMismatches = 0;

        await using var command = new NpgsqlCommand(
            MorphologySql.SelectSegmentsForRenderProvenance,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var formBuckwalter = reader.IsDBNull(1) ? null : reader.GetString(1);
            var formArabicNormalized = reader.IsDBNull(2) ? null : reader.GetString(2);
            var arabicRenderTier = reader.IsDBNull(3) ? null : reader.GetString(3);
            var arabicRenderSource = reader.IsDBNull(4) ? null : reader.GetString(4);

            if (formArabicNormalized is not null && string.IsNullOrWhiteSpace(formBuckwalter))
            {
                missingBuckwalter++;
                continue;
            }

            if (string.IsNullOrEmpty(formBuckwalter))
            {
                continue;
            }

            var (expectedArabic, expectedTier) = renderer.Render(formBuckwalter);

            if (!string.Equals(arabicRenderSource, MorphologyInvariants.RenderSource, StringComparison.Ordinal))
            {
                sourceViolations++;
            }

            if (!string.Equals(formArabicNormalized, expectedArabic, StringComparison.Ordinal))
            {
                renderMismatches++;
            }

            if (!string.Equals(arabicRenderTier, expectedTier, StringComparison.Ordinal))
            {
                tierMismatches++;
            }
        }

        return new RenderProvenanceCounts(
            missingBuckwalter,
            sourceViolations,
            renderMismatches,
            tierMismatches);
    }

    private static Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct) =>
        MorphologyCommandExecutor.ExecuteScalarIntAsync(connection, transaction, sql, ct);

    private static string FormatInt(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private sealed record RenderProvenanceCounts(
        int MissingBuckwalter,
        int SourceViolations,
        int RenderMismatches,
        int TierMismatches)
    {
        public bool Passed =>
            MissingBuckwalter == 0
            && SourceViolations == 0
            && RenderMismatches == 0
            && TierMismatches == 0;
    }
}
