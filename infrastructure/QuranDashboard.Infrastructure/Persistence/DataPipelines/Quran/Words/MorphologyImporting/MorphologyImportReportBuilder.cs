using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;
using QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

internal static class MorphologyImportReportBuilder
{
    private const string FailVerdict = MorphologyImportConstants.FailVerdict;
    private const string MultiStemReportPath =
        "resources/report/words-morphology/multi-stem-words-report.md";

    public static async Task<MorphologyImportTotals> GatherTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        var morphologyRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountMorphologyRows, ct);
        var segmentRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountSegmentRows, ct);
        var rootRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountRootRows, ct);
        var lemmaRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountLemmaRows, ct);
        var lemmaAnalysisRows = await ExecuteScalarIntAsync(connection, transaction, MorphologySql.CountLemmaAnalysisRows, ct);
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
            lemmaAnalysisRows,
            stemRows,
            posTagRows,
            readableWords,
            emptyFormRenders,
            tierCounts);
    }

    public static MorphologyImportResult BuildUnknownPosResult(
        DateTimeOffset runAtUtc, bool force, MorphologySourceData source, MorphologyCheckResult posCheck)
    {
        var unknownList = string.Join(", ", source.UnknownPosCodes);

        return new MorphologyImportResult(
            runAtUtc,
            FailVerdict,
            Persisted: false,
            force,
            BuildAttemptedTotals(source),
            [posCheck],
            Warnings: [],
            Errors: [$"MORPH-POS-RESOLVES: source contains POS codes absent from the controlled vocabulary: {unknownList}."],
            InfoNotes: ["Import refused before any write: unknown POS codes would violate the quran_pos_tags foreign keys; no morphology rows were written."],
            source.CorrectionSummary);
    }

    public static List<string> BuildWarnings(MorphologyImportTotals totals, MorphologySourceData source)
    {
        var stats = source.RenderStats;
        var warnings = new List<string>
        {
            $"{MorphologyInvariants.CheckDimCounts}: roots={totals.RootRows}, lemmas={totals.LemmaRows}, "
            + $"lemma_analyses={totals.LemmaAnalysisRows}, stems={totals.StemRows}."
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

        warnings.Add(FormatListWarning(
            "MORPH-SEG-DIM-ISSUES",
            "segment dimension resolver issue(s)",
            source.SegmentDimensionIssues
                .Select(issue => $"{issue.SegmentLocation} [{issue.CheckId}] {issue.Message}")
                .ToList()));

        warnings.Add(FormatListWarning("MORPH-SEG-REVIEW-LIST", "review-tier form(s)", stats.ReviewTierForms));
        warnings.Add(FormatListWarning("MORPH-SEG-MULTIWORD-LIST", "multiword form(s)", stats.MultiwordForms));
        warnings.Add(FormatListWarning("MORPH-SEG-EMPTY-LIST", "empty-form segment(s) → NULL", stats.EmptyFormLocations));

        var multiStemWarning = BuildMultiStemWarning(source);
        if (multiStemWarning is not null)
        {
            warnings.Add(multiStemWarning);
        }

        if (source.CharsetWarnings.Count > 0)
        {
            warnings.AddRange(source.CharsetWarnings);
        }

        return warnings;
    }

    private static MorphologyImportTotals BuildAttemptedTotals(MorphologySourceData source)
    {
        var segments = source.Words.SelectMany(word => word.Segments).ToList();

        var tierCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["clean"] = 0,
            ["quranic_marks"] = 0,
            ["review"] = 0,
            ["multiword"] = 0
        };

        foreach (var segment in segments)
        {
            if (segment.RenderTier is not null && tierCounts.ContainsKey(segment.RenderTier))
            {
                tierCounts[segment.RenderTier]++;
            }
        }

        return new MorphologyImportTotals(
            source.Words.Count,
            segments.Count,
            source.ResolvedRoots.Count,
            source.ResolvedLemmas.Count,
            source.LemmaAnalyses?.Count ?? 0,
            source.ResolvedStems.Count,
            PosTagSeed.GetAll().Count,
            source.Words.Count,
            source.RenderStats.EmptyFormLocations.Count,
            tierCounts);
    }

    private static string FormatListWarning(string id, string label, IReadOnlyList<string> items) =>
        items.Count == 0
            ? $"{id}: 0 {label}."
            : $"{id}: {items.Count} {label}: {string.Join(", ", items)}.";

    private static string? BuildMultiStemWarning(MorphologySourceData source)
    {
        var multiStemWords = source.Words
            .Select(word => new
            {
                Word = word,
                Stems = word.Segments
                    .Where(segment => string.Equals(segment.Kind, "STEM", StringComparison.Ordinal))
                    .OrderBy(segment => segment.SegmentNumber)
                    .ToList()
            })
            .Where(item => item.Stems.Count > 1)
            .ToList();

        if (multiStemWords.Count == 0)
        {
            return null;
        }

        var pairSummaries = multiStemWords
            .GroupBy(item => string.Join("+", item.Stems.Select(stem => stem.Pos)), StringComparer.Ordinal)
            .Select(group => new
            {
                Pair = group.Key,
                Count = group.Count(),
                Example = group.OrderBy(item => item.Word.Location, StringComparer.Ordinal).First()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Pair, StringComparer.Ordinal)
            .ToList();

        var pairs = string.Join(", ", pairSummaries.Select(item => $"{item.Pair}={item.Count}"));
        var examples = string.Join("; ", pairSummaries.Take(5).Select(item =>
            $"{item.Pair} e.g. {item.Example.Word.Location}"));

        return $"MORPH-MULTI-STEM-LIST: {multiStemWords.Count} multi-STEM word(s); " +
               $"POS pairs: {pairs}; representative examples: {examples}; " +
               $"full investigation report: {MultiStemReportPath}.";
    }

    private static Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct) =>
        MorphologyCommandExecutor.ExecuteScalarIntAsync(connection, transaction, sql, ct);
}
