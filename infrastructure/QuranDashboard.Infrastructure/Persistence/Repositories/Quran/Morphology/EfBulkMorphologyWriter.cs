using System.Data;
using System.Globalization;
using Npgsql;
using NpgsqlTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;
using QuranDashboard.Domain.Quran.Words.Morphology;
using QuranDashboard.Infrastructure.Files.Quran.Morphology;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Morphology;

public sealed class EfBulkMorphologyWriter : IMorphologyImportWriter
{
    private const int CommandTimeoutSeconds = 600;
    private const string PassVerdict = "pass";
    private const string FailVerdict = "fail";
    private const string HardSeverity = "hard";

    private readonly QuranDashboardDbContext dbContext;

    public EfBulkMorphologyWriter(QuranDashboardDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct)
    {
        return await dbContext.WordMorphologies.AnyAsync(ct)
            || await dbContext.WordMorphologySegments.AnyAsync(ct)
            || await dbContext.QuranRoots.AnyAsync(ct)
            || await dbContext.QuranLemmas.AnyAsync(ct)
            || await dbContext.QuranStems.AnyAsync(ct)
            || await dbContext.PosTags.AnyAsync(ct);
    }

    public async Task<MorphologyImportResult> ImportAsync(
        MorphologySourceData source,
        bool force,
        int expectedReadableWords,
        Func<CancellationToken, Task<bool>> sourceUnchangedCheck,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceUnchangedCheck);

        if (!force && await AnyTargetTableHasDataAsync(ct))
        {
            throw new InvalidOperationException(MorphologyInvariants.TargetsNotEmpty);
        }

        var runAtUtc = DateTimeOffset.UtcNow;
        var wordIdsByLocation = await ReadReadableWordIdsAsync(ct);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new InvalidOperationException("Expected an Npgsql connection for morphology import.");
        }

        await using var transaction = await npgsqlConnection.BeginTransactionAsync(ct);

        try
        {
            if (force)
            {
                await ExecuteNonQueryAsync(
                    npgsqlConnection, transaction, MorphologySql.TruncateMorphologyTables, ct);
            }

            await CopyPosTagsAsync(npgsqlConnection, ct);
            await CopyRootsAsync(npgsqlConnection, source, ct);
            await CopyLemmasAsync(npgsqlConnection, source, ct);
            await CopyStemsAsync(npgsqlConnection, source, ct);
            await CopyMorphologyAsync(npgsqlConnection, source, wordIdsByLocation, ct);
            await CopySegmentsAsync(npgsqlConnection, source, wordIdsByLocation, ct);

            var totals = await GatherTotalsAsync(npgsqlConnection, transaction, ct);
            var checks = await RunAllHardChecksAsync(
                npgsqlConnection,
                transaction,
                expectedReadableWords,
                source,
                ct);

            var sourceUnchanged = await sourceUnchangedCheck(ct);
            checks.Add(new MorphologyCheckResult(
                MorphologyInvariants.CheckSourceUnchanged,
                HardSeverity,
                "local source files match manifest.json size/sha256 before and after run",
                sourceUnchanged ? "unchanged" : "changed",
                sourceUnchanged));

            var warnings = BuildWarnings(totals, source);

            var hardChecks = checks.Where(check => check.Severity == HardSeverity).ToList();
            var allHardPassed = hardChecks.All(check => check.Passed);

            if (allHardPassed)
            {
                await transaction.CommitAsync(ct);

                return new MorphologyImportResult(
                    runAtUtc,
                    PassVerdict,
                    Persisted: true,
                    force,
                    totals,
                    checks,
                    warnings,
                    Errors: [],
                    InfoNotes: ["Morphology import committed; all hard checks passed."]);
            }

            await transaction.RollbackAsync(ct);

            var errors = hardChecks
                .Where(check => !check.Passed)
                .Select(check => $"{check.Id}: expected {check.Expected}, observed {check.Observed}")
                .ToList();

            return new MorphologyImportResult(
                runAtUtc,
                FailVerdict,
                Persisted: false,
                force,
                totals,
                checks,
                warnings,
                errors,
                InfoNotes: ["Totals reflect the attempted import before rollback; no morphology rows were persisted."]);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Dictionary<string, int>> ReadReadableWordIdsAsync(CancellationToken ct)
    {
        var rows = await dbContext.QuranWords
            .AsNoTracking()
            .Where(word => !word.IsAyahMarker)
            .Select(word => new { word.Location, word.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.Location, row => row.Id, StringComparer.Ordinal);
    }

    private static async Task CopyPosTagsAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_pos_tags (code, arabic_label, english_label, category, sort_order, description)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var tag in PosTagSeed.GetAll())
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(tag.Code, ct);
            await importer.WriteAsync(tag.ArabicLabel, ct);
            await importer.WriteAsync(tag.EnglishLabel, ct);
            await importer.WriteAsync(tag.Category, ct);
            await importer.WriteAsync(tag.SortOrder, ct);
            await importer.WriteAsync(tag.Description, NpgsqlDbType.Text, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyRootsAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_roots (id, root_text, root_buckwalter, words_count, distinct_lemmas_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var root in source.ResolvedRoots)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(root.AssignedId, ct);
            await importer.WriteAsync(root.RootText, ct);
            await importer.WriteAsync(root.RootBuckwalter, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(root.WordsCount, ct);
            await importer.WriteAsync(root.DistinctLemmasCount, ct);
            await importer.WriteAsync(root.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyLemmasAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_lemmas (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var lemma in source.ResolvedLemmas)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(lemma.AssignedId, ct);
            await importer.WriteAsync(lemma.LemmaText, ct);
            await importer.WriteAsync(lemma.LemmaBuckwalter, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(lemma.RootId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(lemma.WordsCount, ct);
            await importer.WriteAsync(lemma.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyStemsAsync(
        NpgsqlConnection connection, MorphologySourceData source, CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_stems (id, stem_text, words_count, first_word_order_in_mushaf)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var stem in source.ResolvedStems)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(stem.AssignedId, ct);
            await importer.WriteAsync(stem.StemText, ct);
            await importer.WriteAsync(stem.WordsCount, ct);
            await importer.WriteAsync(stem.FirstWordOrderInMushaf, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopyMorphologyAsync(
        NpgsqlConnection connection,
        MorphologySourceData source,
        IReadOnlyDictionary<string, int> wordIdsByLocation,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_word_morphology (
                quran_word_id, location, head_pos, segment_count,
                root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice,
                case_feature, head_features_json)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var word in source.Words)
        {
            if (!wordIdsByLocation.TryGetValue(word.Location, out var quranWordId))
            {
                throw new InvalidDataException($"Readable word location '{word.Location}' was not found in quran_words.");
            }

            await importer.StartRowAsync(ct);
            await importer.WriteAsync(quranWordId, ct);
            await importer.WriteAsync(word.Location, ct);
            await importer.WriteAsync(word.HeadPos, ct);
            await importer.WriteAsync((short)word.Segments.Count, ct);
            await importer.WriteAsync(word.RootId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.LemmaId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.StemId, NpgsqlDbType.Integer, ct);
            await importer.WriteAsync(word.IsVerb, ct);
            await importer.WriteAsync(word.VerbTense, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.VerbVoice, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.CaseFeature, NpgsqlDbType.Text, ct);
            await importer.WriteAsync(word.HeadFeaturesJson, NpgsqlDbType.Jsonb, ct);
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task CopySegmentsAsync(
        NpgsqlConnection connection,
        MorphologySourceData source,
        IReadOnlyDictionary<string, int> wordIdsByLocation,
        CancellationToken ct)
    {
        const string copyCommand = """
            COPY quran_word_morphology_segments (
                quran_word_id, segment_location, segment_number, kind, pos,
                form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source,
                root_buckwalter, lemma_buckwalter, features_raw, features_json)
            FROM STDIN (FORMAT BINARY)
            """;

        await using var importer = await connection.BeginBinaryImportAsync(copyCommand, ct);

        foreach (var word in source.Words)
        {
            if (!wordIdsByLocation.TryGetValue(word.Location, out var quranWordId))
            {
                throw new InvalidDataException($"Readable word location '{word.Location}' was not found in quran_words.");
            }

            foreach (var segment in word.Segments)
            {
                var segmentLocation = $"{word.Location}:{segment.SegmentNumber}";

                await importer.StartRowAsync(ct);
                await importer.WriteAsync(quranWordId, ct);
                await importer.WriteAsync(segmentLocation, ct);
                await importer.WriteAsync(segment.SegmentNumber, ct);
                await importer.WriteAsync(segment.Kind, ct);
                await importer.WriteAsync(segment.Pos, ct);
                await importer.WriteAsync(segment.FormBuckwalter, ct);
                await importer.WriteAsync(segment.FormArabicNormalized, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.RenderTier, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.RenderSource, ct);
                await importer.WriteAsync(segment.RootBuckwalter, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.LemmaBuckwalter, NpgsqlDbType.Text, ct);
                await importer.WriteAsync(segment.FeaturesRaw, ct);
                await importer.WriteAsync(segment.FeaturesJson, NpgsqlDbType.Jsonb, ct);
            }
        }

        await importer.CompleteAsync(ct);
    }

    private static async Task<List<MorphologyCheckResult>> RunAllHardChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int expectedReadableWords,
        MorphologySourceData source,
        CancellationToken ct)
    {
        var checks = new List<MorphologyCheckResult>();

        await AddUs1ChecksAsync(checks, connection, transaction, expectedReadableWords, ct);
        await AddUs3ChecksAsync(checks, connection, transaction, source, ct);

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
            "one STEM per word; head_pos = STEM pos",
            posViolations == 0 ? "0 violations" : $"{FormatInt(posViolations)} violation(s)",
            posViolations == 0));

        var verbViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckVerbFeatureViolations, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-VERB-FEATURE-CONSISTENCY",
            HardSeverity,
            "verbs: one tense + voice; non-verbs: null verb fields",
            verbViolations == 0 ? "0 violations" : $"{FormatInt(verbViolations)} violation(s)",
            verbViolations == 0));
    }

    private static async Task AddUs3ChecksAsync(
        List<MorphologyCheckResult> checks,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        MorphologySourceData source,
        CancellationToken ct)
    {
        var charsetViolations = source.CharsetWarnings.Count;
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-CHARSET",
            HardSeverity,
            "0 unmapped",
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

        var uthmaniViolations = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegNotUthmani, ct);
        var missingBuckwalter = await ExecuteScalarIntAsync(
            connection, transaction, MorphologySql.CheckSegBuckwalterPresent, ct);
        checks.Add(new MorphologyCheckResult(
            "MORPH-SEG-NOT-UTHMANI",
            HardSeverity,
            "render never equals text_uthmani/qpc_glyph; form_buckwalter always present",
            $"uthmani_match={FormatInt(uthmaniViolations)}, missing_buckwalter={FormatInt(missingBuckwalter)}",
            uthmaniViolations == 0 && missingBuckwalter == 0));

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

    private static async Task<MorphologyImportTotals> GatherTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var morphologyRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountMorphologyRows, ct);
        var segmentRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountSegmentRows, ct);
        var rootRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountRootRows, ct);
        var lemmaRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountLemmaRows, ct);
        var stemRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountStemRows, ct);
        var posTagRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountPosTagRows, ct);
        var readableWords = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CheckReadableWordsCount, ct);
        var emptyFormRenders = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountEmptyFormRenders, ct);

        var clean = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountTierClean, ct);
        var quranicMarks = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountTierQuranicMarks, ct);
        var review = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountTierReview, ct);
        var multiword = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountTierMultiword, ct);

        var tierCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["clean"] = clean,
            ["quranic_marks"] = quranicMarks,
            ["review"] = review,
            ["multiword"] = multiword
        };

        return new MorphologyImportTotals(
            morphologyRows,
            segmentRows,
            rootRows,
            lemmaRows,
            stemRows,
            posTagRows,
            readableWords,
            emptyFormRenders,
            tierCounts);
    }

    private static List<string> BuildWarnings(MorphologyImportTotals totals, MorphologySourceData source)
    {
        var stats = source.RenderStats;
        var warnings = new List<string>
        {
            $"{MorphologyInvariants.CheckDimCounts}: roots={totals.RootRows}, lemmas={totals.LemmaRows}, stems={totals.StemRows}."
        };

        var totalRendered = totals.SegmentRows - totals.EmptyFormRenders;
        if (totalRendered > 0)
        {
            warnings.Add($"MORPH-SEG-TIER-DIST: clean={totals.RenderTierCounts.GetValueOrDefault("clean", 0)}, " +
                         $"quranic_marks={totals.RenderTierCounts.GetValueOrDefault("quranic_marks", 0)}, " +
                         $"review={totals.RenderTierCounts.GetValueOrDefault("review", 0)}, " +
                         $"multiword={totals.RenderTierCounts.GetValueOrDefault("multiword", 0)}.");
        }

        if (stats.WholeWordAgreementTotal > 0)
        {
            var rate = (double)stats.WholeWordAgreementMatches / stats.WholeWordAgreementTotal;
            warnings.Add(
                $"MORPH-SEG-WORD-AGREEMENT: whole-word agreement = {rate.ToString("P2", CultureInfo.InvariantCulture)} " +
                $"({stats.WholeWordAgreementMatches}/{stats.WholeWordAgreementTotal}); baseline ≈ 79.83% (informational).");
        }

        warnings.Add(FormatListWarning("MORPH-SEG-REVIEW-LIST", "review-tier form(s)", stats.ReviewTierForms));
        warnings.Add(FormatListWarning("MORPH-SEG-MULTIWORD-LIST", "multiword form(s)", stats.MultiwordForms));
        warnings.Add(FormatListWarning("MORPH-SEG-EMPTY-LIST", "empty-form segment(s) → NULL", stats.EmptyFormLocations));

        if (source.CharsetWarnings.Count > 0)
        {
            warnings.AddRange(source.CharsetWarnings);
        }

        return warnings;
    }

    private static string FormatListWarning(string id, string label, IReadOnlyList<string> items) =>
        items.Count == 0
            ? $"{id}: 0 {label}."
            : $"{id}: {items.Count} {label}: {string.Join(", ", items)}.";

    private static string FormatInt(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    private static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(ct);
    }
}
