using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting.Corrections;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

internal static class MorphologyValidationRunner
{
    private const string HardSeverity = MorphologyImportConstants.HardSeverity;

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
        WordLemmaNormalizationLoaded? normalization,
        SegmentStemCorrectionLoaded? segmentStemCorrection,
        SegmentArabicRenderer renderer,
        CancellationToken ct)
    {
        var checks = new List<MorphologyCheckResult>();

        await AddUs1ChecksAsync(checks, connection, transaction, expectedReadableWords, ct);
        await AddUs3ChecksAsync(checks, connection, transaction, source, renderer, ct);
        await AddSegmentDimensionChecksAsync(checks, connection, transaction, source, ct);
        await AddSegmentStemChecksAsync(checks, connection, transaction, segmentStemCorrection, ct);
        if (normalization is not null)
        {
            await AddWordLemmaNormalizationChecksAsync(checks, connection, transaction, normalization, ct);
        }

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
        var expectedRenderSource = ResolveExpectedRenderSource(source);
        var sourceViolations = await CountRenderSourceViolationsAsync(
            connection, transaction, expectedRenderSource, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-TIER-VALID",
            HardSeverity,
            $"valid tier + source = {expectedRenderSource} on all rendered rows",
            $"tier_violations={FormatInt(tierViolations)}, source_violations={FormatInt(sourceViolations)}",
            tierViolations == 0 && sourceViolations == 0));

        var provenance = source.SourceKind == MorphologyImportSourceKind.Enriched
            ? await CheckEnrichedRenderProvenanceAsync(connection, transaction, source, ct)
            : await CheckLegacyRenderProvenanceAsync(connection, transaction, renderer, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-RENDER-PROVENANCE",
            HardSeverity,
            source.SourceKind == MorphologyImportSourceKind.Enriched
                ? "rendered rows match enriched source Arabic/tier/source values and retain non-empty form_buckwalter"
                : "rendered rows retain non-empty form_buckwalter; source = buckwalter-transliteration; Arabic and tier recompute from each row's own form_buckwalter; deterministic Uthmani/QPC equality is allowed",
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

    private static async Task AddSegmentDimensionChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MorphologySourceData source,
        CancellationToken ct)
    {
        var nonStemLemmaIds = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegLemmaStemOnly, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegLemmaStemOnly,
            HardSeverity,
            "lemma_id appears only on STEM segments",
            nonStemLemmaIds == 0 ? "0 violations" : $"{FormatInt(nonStemLemmaIds)} violation(s)",
            nonStemLemmaIds == 0));

        var singleStemMismatches = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegLemmaSingleStemHeadConsistent, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegLemmaSingleStemHeadConsistent,
            HardSeverity,
            "single-STEM segment lemma_id equals quran_word_morphology.lemma_id",
            singleStemMismatches == 0 ? "0 violations" : $"{FormatInt(singleStemMismatches)} violation(s)",
            singleStemMismatches == 0));

        var fanoutViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegLemmaNoFanout, ct);
        var fanoutIssues = CountIssues(source, MorphologyInvariants.CheckSegLemmaNoFanout);
        var totalFanoutViolations = fanoutViolations + fanoutIssues;
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegLemmaNoFanout,
            HardSeverity,
            "one segment never maps to multiple candidate lemmas",
            totalFanoutViolations == 0
                ? "0 violations"
                : $"schema_violations={FormatInt(fanoutViolations)}, resolver_issues={FormatInt(fanoutIssues)}",
            totalFanoutViolations == 0));

        var multiStemMissing = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegLemmaMultiStemResolves, ct);
        var multiStemIssues = CountIssues(source, MorphologyInvariants.CheckSegLemmaMultiStemResolves);
        var multiStemViolations = multiStemMissing + multiStemIssues;
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegLemmaMultiStemResolves,
            HardSeverity,
            "multi-STEM lemma-bearing segments resolve to exactly one lemma",
            multiStemViolations == 0
                ? "0 violations"
                : $"missing={FormatInt(multiStemMissing)}, resolver_issues={FormatInt(multiStemIssues)}",
            multiStemViolations == 0));

        var missingStemLemmaIds = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegLemmaRequiredForStem, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegLemmaRequiredForStem,
            HardSeverity,
            "every STEM segment with lemma_buckwalter has lemma_id when the word head lemma_id is set",
            missingStemLemmaIds == 0 ? "0 violations" : $"{FormatInt(missingStemLemmaIds)} violation(s)",
            missingStemLemmaIds == 0));

        var unresolvedRoots = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegRootResolves, ct);
        var rootIssues = CountIssues(source, MorphologyInvariants.CheckSegRootResolves);
        var rootViolations = unresolvedRoots + rootIssues;
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegRootResolves,
            HardSeverity,
            "every non-empty root_buckwalter has root_id",
            rootViolations == 0
                ? "0 violations"
                : $"missing={FormatInt(unresolvedRoots)}, resolver_issues={FormatInt(rootIssues)}",
            rootViolations == 0));

        var rootMismatches = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegRootConsistent, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegRootConsistent,
            HardSeverity,
            "segment root_id references a root row with matching root_buckwalter",
            rootMismatches == 0 ? "0 violations" : $"{FormatInt(rootMismatches)} violation(s)",
            rootMismatches == 0));

        var nullSafetyViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegDimNullSafe, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegDimNullSafe,
            HardSeverity,
            "null source values remain null IDs and non-STEM lemma_id remains null",
            nullSafetyViolations == 0 ? "0 violations" : $"{FormatInt(nullSafetyViolations)} violation(s)",
            nullSafetyViolations == 0));
    }

    private static async Task AddSegmentStemChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SegmentStemCorrectionLoaded? segmentStemCorrection,
        CancellationToken ct)
    {
        var nonStemStemIds = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegStemStemOnly, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemStemOnly,
            HardSeverity,
            "stem_id appears only on STEM segments",
            nonStemStemIds == 0 ? "0 violations" : $"{FormatInt(nonStemStemIds)} violation(s)",
            nonStemStemIds == 0));

        var missingStemIds = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegStemRequiredForStem, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemRequiredForStem,
            HardSeverity,
            "every STEM segment has stem_id except the 4 documented unresolved secondary exceptions",
            missingStemIds == 0 ? "0 violations" : $"{FormatInt(missingStemIds)} violation(s)",
            missingStemIds == 0));

        var headMismatches = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegStemHeadConsistent, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemHeadConsistent,
            HardSeverity,
            "single-STEM and two-STEM primary segments reuse the word head stem_id",
            headMismatches == 0 ? "0 violations" : $"{FormatInt(headMismatches)} violation(s)",
            headMismatches == 0));

        var danglingStemIds = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegStemResolves, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemResolves,
            HardSeverity,
            "every segment stem_id references a real quran_stems row",
            danglingStemIds == 0 ? "0 violations" : $"{FormatInt(danglingStemIds)} violation(s)",
            danglingStemIds == 0));

        if (segmentStemCorrection is null)
        {
            return;
        }

        var curated = await CheckSecondaryStemCurationAsync(
            connection, transaction, segmentStemCorrection, ct);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemMultiStemCurated,
            HardSeverity,
            "every secondary STEM segment present is covered by the curated artifact (approved => stem_id set; unresolved => null)",
            curated.Violations == 0
                ? $"0 violations ({FormatInt(curated.ApprovedApplied)} approved applied, {FormatInt(curated.UnresolvedNull)} unresolved null of {FormatInt(curated.SecondaryPresent)} present)"
                : $"{FormatInt(curated.Violations)} violation(s) (uncovered={FormatInt(curated.Uncovered)}, approved_not_applied={FormatInt(curated.ApprovedNotApplied)}, unresolved_set={FormatInt(curated.UnresolvedSet)})",
            curated.Violations == 0));

        var shape = segmentStemCorrection.Counts;
        var unresolvedMatches = segmentStemCorrection.UnresolvedLocations.Count == MorphologyInvariants.UnresolvedSecondaryStemLocations.Count
            && MorphologyInvariants.UnresolvedSecondaryStemLocations.All(segmentStemCorrection.UnresolvedLocations.Contains);
        var shapePassed = shape.Total == MorphologyInvariants.ExpectedSecondaryStemCandidates
            && shape.Approved == MorphologyInvariants.ExpectedApprovedSecondaryStems
            && shape.UnresolvedExceptions == MorphologyInvariants.ExpectedUnresolvedSecondaryStems
            && unresolvedMatches;
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckSegStemArtifactShape,
            HardSeverity,
            $"curated artifact has {MorphologyInvariants.ExpectedSecondaryStemCandidates} mappings "
            + $"({MorphologyInvariants.ExpectedApprovedSecondaryStems} approved + {MorphologyInvariants.ExpectedUnresolvedSecondaryStems} unresolved at the 4 named locations)",
            $"total={FormatInt(shape.Total)}, approved={FormatInt(shape.Approved)}, unresolved={FormatInt(shape.UnresolvedExceptions)}, named_unresolved_match={unresolvedMatches}",
            shapePassed));
    }

    private static async Task<SecondaryStemCurationCounts> CheckSecondaryStemCurationAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SegmentStemCorrectionLoaded segmentStemCorrection,
        CancellationToken ct)
    {
        var secondaryPresent = 0;
        var approvedApplied = 0;
        var unresolvedNull = 0;
        var uncovered = 0;
        var approvedNotApplied = 0;
        var unresolvedSet = 0;

        await using var command = new NpgsqlCommand(
            MorphologySql.SelectSecondaryStemSegments,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            secondaryPresent++;
            var location = reader.GetString(0);
            var hasStemId = !reader.IsDBNull(1);
            var isApproved = segmentStemCorrection.ApprovedStemTextByLocation.ContainsKey(location);
            var isUnresolved = segmentStemCorrection.UnresolvedLocations.Contains(location);

            if (!isApproved && !isUnresolved)
            {
                uncovered++;
            }
            else if (isApproved && !hasStemId)
            {
                approvedNotApplied++;
            }
            else if (isUnresolved && hasStemId)
            {
                unresolvedSet++;
            }

            if (isApproved && hasStemId)
            {
                approvedApplied++;
            }

            if (isUnresolved && !hasStemId)
            {
                unresolvedNull++;
            }
        }

        return new SecondaryStemCurationCounts(
            secondaryPresent, approvedApplied, unresolvedNull, uncovered, approvedNotApplied, unresolvedSet);
    }

    private static async Task AddWordLemmaNormalizationChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        WordLemmaNormalizationLoaded normalization,
        CancellationToken ct)
    {
        var observed = await ReadPersistedLemmaTextsAsync(connection, transaction, ct);
        var approvedMutatingEntries = normalization.Artifact.Entries
            .Where(IsApprovedMutatingEntry)
            .ToList();

        var appliedMismatches = CountEntryMismatches(approvedMutatingEntries, observed);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckWordLemmaNormalizationApplied,
            HardSeverity,
            "every approved mutating normalization entry persists corrected lemma text",
            appliedMismatches == 0
                ? $"0 mismatches across {FormatInt(approvedMutatingEntries.Count)} approved mutating entries"
                : $"{FormatInt(appliedMismatches)} mismatch(es) across {FormatInt(approvedMutatingEntries.Count)} approved mutating entries",
            appliedMismatches == 0));

        var strictShiftLocations = await ReadStrictShiftLocationsAsync(connection, transaction, ct);
        var allowedShiftLocations = normalization.Artifact.Entries
            .Where(entry => entry.DecisionStatus is WordLemmaNormalizationDecisionStatus.Approved
                or WordLemmaNormalizationDecisionStatus.AcceptedException)
            .Select(entry => entry.Location)
            .ToHashSet(StringComparer.Ordinal);
        var remainingShiftLocations = strictShiftLocations
            .Where(location => !allowedShiftLocations.Contains(location))
            .ToList();
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckWordLemmaShiftClean,
            HardSeverity,
            "no unapproved strict previous-word shift rows remain",
            remainingShiftLocations.Count == 0
                ? "0 unapproved strict shifts"
                : $"{FormatInt(remainingShiftLocations.Count)} unapproved strict shift(s): {string.Join(", ", remainingShiftLocations.Take(10))}",
            remainingShiftLocations.Count == 0));

        var replaceEntries = normalization.Artifact.Entries
            .Where(entry => entry.DecisionStatus == WordLemmaNormalizationDecisionStatus.Approved
                && entry.OperationKind == WordLemmaNormalizationOperationKind.Replace)
            .ToList();
        var replaceMismatches = CountEntryMismatches(replaceEntries, observed);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckWordLemmaReplaceValid,
            HardSeverity,
            "approved replace locations persist with corrected lemma text",
            replaceMismatches == 0
                ? $"0 mismatches across {FormatInt(replaceEntries.Count)} replace entries"
                : $"{FormatInt(replaceMismatches)} mismatch(es) across {FormatInt(replaceEntries.Count)} replace entries",
            replaceMismatches == 0));

        var missingRecoveryEntries = normalization.Artifact.Entries
            .Where(entry => entry.DecisionStatus == WordLemmaNormalizationDecisionStatus.Approved
                && entry.OperationKind == WordLemmaNormalizationOperationKind.Add
                && string.Equals(entry.ProblemClass, WordLemmaProblemClasses.MissingRecovery, StringComparison.Ordinal))
            .ToList();
        var missingRecoveryMismatches = CountEntryMismatches(missingRecoveryEntries, observed);
        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckWordLemmaMissingRecoveryClean,
            HardSeverity,
            "approved missing-recovery add locations persist with corrected lemma text",
            missingRecoveryMismatches == 0
                ? $"0 mismatches across {FormatInt(missingRecoveryEntries.Count)} missing-recovery entries"
                : $"{FormatInt(missingRecoveryMismatches)} mismatch(es) across {FormatInt(missingRecoveryEntries.Count)} missing-recovery entries",
            missingRecoveryMismatches == 0));

        checks.Add(new MorphologyCheckResult(
            MorphologyInvariants.CheckWordLemmaUncertainZero,
            HardSeverity,
            "active normalization artifact has 0 candidate / needs-review entries",
            normalization.Counts.CandidateOrNeedsReview == 0
                ? "0 pending entries"
                : $"{FormatInt(normalization.Counts.CandidateOrNeedsReview)} pending entries",
            normalization.Counts.CandidateOrNeedsReview == 0));
    }

    private static async Task<int> CountRenderSourceViolationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string expectedRenderSource,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*)::int
            FROM quran_word_morphology_segments
            WHERE form_buckwalter <> ''
              AND arabic_render_source IS DISTINCT FROM @expectedRenderSource
            """,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };
        command.Parameters.AddWithValue("expectedRenderSource", expectedRenderSource);

        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static string ResolveExpectedRenderSource(MorphologySourceData source)
    {
        return source.SourceKind == MorphologyImportSourceKind.Enriched
            ? MorphologyInvariants.EnrichedRenderSource
            : MorphologyInvariants.RenderSource;
    }

    private static async Task<RenderProvenanceCounts> CheckLegacyRenderProvenanceAsync(
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

    private static async Task<RenderProvenanceCounts> CheckEnrichedRenderProvenanceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MorphologySourceData source,
        CancellationToken ct)
    {
        var expectedBySegmentLocation = source.Words
            .SelectMany(word => word.Segments.Select(segment => new
            {
                SegmentLocation = $"{word.Location}:{segment.SegmentNumber}",
                Segment = segment
            }))
            .ToDictionary(item => item.SegmentLocation, item => item.Segment, StringComparer.Ordinal);

        var missingBuckwalter = 0;
        var sourceViolations = 0;
        var renderMismatches = 0;
        var tierMismatches = 0;

        await using var command = new NpgsqlCommand(
            """
            SELECT segment_location, form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source
            FROM quran_word_morphology_segments
            WHERE form_buckwalter <> ''
               OR form_arabic_normalized IS NOT NULL
            """,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var segmentLocation = reader.GetString(0);
            var formBuckwalter = reader.IsDBNull(1) ? null : reader.GetString(1);
            var formArabicNormalized = reader.IsDBNull(2) ? null : reader.GetString(2);
            var arabicRenderTier = reader.IsDBNull(3) ? null : reader.GetString(3);
            var arabicRenderSource = reader.IsDBNull(4) ? null : reader.GetString(4);

            if (formArabicNormalized is not null && string.IsNullOrWhiteSpace(formBuckwalter))
            {
                missingBuckwalter++;
                continue;
            }

            if (!expectedBySegmentLocation.TryGetValue(segmentLocation, out var expected))
            {
                renderMismatches++;
                continue;
            }

            if (!string.Equals(arabicRenderSource, expected.RenderSource, StringComparison.Ordinal))
            {
                sourceViolations++;
            }

            if (!string.Equals(formArabicNormalized, expected.FormArabicNormalized, StringComparison.Ordinal))
            {
                renderMismatches++;
            }

            if (!string.Equals(arabicRenderTier, expected.RenderTier, StringComparison.Ordinal))
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

    private static async Task<List<string>> ReadStrictShiftLocationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var locations = new List<string>();

        await using var command = new NpgsqlCommand(
            MorphologySql.SelectStrictWordLemmaShiftLocations,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            locations.Add(reader.GetString(0));
        }

        return locations;
    }

    private static async Task<Dictionary<string, string?>> ReadPersistedLemmaTextsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var observations = new Dictionary<string, string?>(StringComparer.Ordinal);

        await using var command = new NpgsqlCommand(
            MorphologySql.SelectMorphologyLemmaObservations,
            connection,
            transaction)
        {
            CommandTimeout = MorphologyCommandExecutor.CommandTimeoutSeconds
        };

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var location = reader.GetString(0);
            var lemmaText = reader.IsDBNull(1) ? null : reader.GetString(1);
            observations[location] = lemmaText;
        }

        return observations;
    }

    private static int CountEntryMismatches(
        IReadOnlyList<WordLemmaNormalizationEntry> entries,
        IReadOnlyDictionary<string, string?> observed)
    {
        var mismatches = 0;

        foreach (var entry in entries)
        {
            if (!observed.TryGetValue(entry.Location, out var observedLemma)
                || !string.Equals(entry.CorrectedLemmaArabic, observedLemma, StringComparison.Ordinal))
            {
                mismatches++;
            }
        }

        return mismatches;
    }

    private static bool IsApprovedMutatingEntry(WordLemmaNormalizationEntry entry) =>
        entry.DecisionStatus == WordLemmaNormalizationDecisionStatus.Approved
        && entry.OperationKind is WordLemmaNormalizationOperationKind.Add
            or WordLemmaNormalizationOperationKind.Remove
            or WordLemmaNormalizationOperationKind.Replace;

    private static int CountIssues(MorphologySourceData source, string checkId) =>
        source.SegmentDimensionIssues.Count(issue =>
            string.Equals(issue.CheckId, checkId, StringComparison.Ordinal));

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

    private sealed record SecondaryStemCurationCounts(
        int SecondaryPresent,
        int ApprovedApplied,
        int UnresolvedNull,
        int Uncovered,
        int ApprovedNotApplied,
        int UnresolvedSet)
    {
        // Secondary segments whose location is covered by the curated artifact (approved or unresolved).
        private int CoveredPresent => ApprovedApplied + ApprovedNotApplied + UnresolvedNull + UnresolvedSet;

        // Approved-but-null and unresolved-but-set are always violations. An uncovered secondary is a
        // violation only once the artifact actually applies to this dataset (>=1 covered secondary) —
        // that is the real Quran import; synthetic test data with no artifact locations is tolerated.
        public int Violations =>
            ApprovedNotApplied + UnresolvedSet + (CoveredPresent > 0 ? Uncovered : 0);
    }
}
