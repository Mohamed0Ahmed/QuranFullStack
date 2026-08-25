using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Words.MorphologyImporting;

public sealed class MarkdownJsonMorphologyReportWriter : IMorphologyReportWriter
{
    private const string MarkdownFileName = "morphology-import-report.md";
    private const string JsonFileName = "morphology-import-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(MorphologyImportResult result, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, JsonFileName);
        var markdownPath = Path.Combine(outputDir, MarkdownFileName);

        var tierCounts = result.Totals.RenderTierCounts;
        var reportDocument = new ReportDocument(
            result.RunAtUtc,
            result.Verdict,
            result.Persisted,
            result.Forced,
            new ReportTotals(
                result.Totals.MorphologyRows,
                result.Totals.SegmentRows,
                result.Totals.RootRows,
                result.Totals.LemmaRows,
                result.Totals.LemmaAnalysisRows,
                result.Totals.StemRows,
                result.Totals.PosTagRows,
                result.Totals.ReadableWords,
                result.Totals.EmptyFormRenders,
                tierCounts.GetValueOrDefault("clean", 0),
                tierCounts.GetValueOrDefault("quranic_marks", 0),
                tierCounts.GetValueOrDefault("review", 0),
                tierCounts.GetValueOrDefault("multiword", 0)),
            result.Checks
                .Select(check => new ReportCheck(
                    check.Id,
                    check.Severity,
                    check.Expected,
                    check.Observed,
                    check.Passed))
                .ToList(),
            result.Warnings.ToList(),
            result.Errors.ToList(),
            result.InfoNotes.ToList(),
            result.CorrectionSummary,
            result.RootFallbackSummary);

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, reportDocument, JsonOptions, ct);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(reportDocument), Encoding.UTF8, ct);
    }

    private static string BuildMarkdown(ReportDocument report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Quran Word Morphology — Import Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunAtUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict.ToUpperInvariant()}");
        builder.AppendLine($"- Persisted: {report.Persisted}");
        builder.AppendLine($"- Forced: {report.Forced}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Table | Rows |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| quran_word_morphology | {report.Totals.MorphologyRows:N0} |");
        builder.AppendLine($"| quran_word_morphology_segments | {report.Totals.SegmentRows:N0} |");
        builder.AppendLine($"| quran_roots | {report.Totals.RootRows:N0} |");
        builder.AppendLine($"| quran_lemmas | {report.Totals.LemmaRows:N0} |");
        builder.AppendLine($"| quran_lemma_analyses | {report.Totals.LemmaAnalysisRows:N0} |");
        builder.AppendLine($"| quran_stems | {report.Totals.StemRows:N0} |");
        builder.AppendLine($"| quran_pos_tags | {report.Totals.PosTagRows:N0} |");
        builder.AppendLine($"| readable words (source) | {report.Totals.ReadableWords:N0} |");
        builder.AppendLine();
        builder.AppendLine("## Render tiers");
        builder.AppendLine();
        builder.AppendLine("| Tier | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| clean | {report.Totals.CleanCount:N0} |");
        builder.AppendLine($"| quranic_marks | {report.Totals.QuranicMarksCount:N0} |");
        builder.AppendLine($"| review | {report.Totals.ReviewCount:N0} |");
        builder.AppendLine($"| multiword | {report.Totals.MultiwordCount:N0} |");
        builder.AppendLine();
        builder.AppendLine($"(empty-form renders → NULL: {report.Totals.EmptyFormRenders})");

        if (report.RootFallbackSummary is not null)
        {
            var fallback = report.RootFallbackSummary;
            builder.AppendLine();
            builder.AppendLine("## Approved root fallback");
            builder.AppendLine();
            builder.AppendLine("- Artifact: `approved-root-fallbacks.json`");
            builder.AppendLine($"- Artifact SHA-256: `{fallback.ArtifactSha256}`");
            builder.AppendLine($"- Source: `{fallback.Source}`");
            builder.AppendLine($"- Expected / applied: {fallback.ExpectedEntries:N0} / {fallback.AppliedEntries:N0}");
            builder.AppendLine($"- Strong / linguistic / lexical: {fallback.StrongEntries:N0} / {fallback.LinguisticEntries:N0} / {fallback.LexicalEntries:N0}");
        }

        if (report.CorrectionSummary is not null)
        {
            var summary = report.CorrectionSummary;
            var sourceUnchanged = report.Checks.FirstOrDefault(check => check.Id == MorphologyInvariants.CheckSourceUnchanged);
            var shiftClean = report.Checks.FirstOrDefault(check => check.Id == MorphologyInvariants.CheckWordLemmaShiftClean);
            builder.AppendLine();
            builder.AppendLine("## Lemma normalization");
            builder.AppendLine();
            builder.AppendLine("- Artifact: `word-lemma-normalization.json`");
            builder.AppendLine($"- Artifact SHA-256: `{summary.ArtifactSha256}`");
            builder.AppendLine($"- Raw QUL lemma SHA-256: `{summary.RawLemmasSha256 ?? string.Empty}`");
            builder.AppendLine($"- Total entries: {summary.TotalEntries:N0}");
            builder.AppendLine($"- Applied add/remove/replace: {summary.AppliedAdd:N0} / {summary.AppliedRemove:N0} / {summary.AppliedReplace:N0}");
            builder.AppendLine($"- Reviewed keep/exception: {summary.ReviewedKeep:N0} / {summary.ReviewedException:N0}");
            builder.AppendLine($"- Failed or skipped: {summary.FailedOrSkipped:N0}");
            builder.AppendLine($"- Remaining unapproved shift count: {ExtractLeadingCount(shiftClean?.Observed) ?? "unknown"}");
            builder.AppendLine($"- Source unchanged: {(sourceUnchanged?.Passed == true ? "yes" : "no")}");

            var original63 = GetProblemClassCount(summary.ProblemClassCounts, "shift-63")
                + GetProblemClassCount(summary.ProblemClassCounts, "shift-63-replace");
            var candidate59 = GetProblemClassCount(summary.ProblemClassCounts, "shift-59");
            var missingRecovery = GetProblemClassCount(summary.ProblemClassCounts, "missing-recovery");
            var uncertain = GetProblemClassCount(summary.ProblemClassCounts, "uncertain");
            var multiStem = GetProblemClassCount(summary.ProblemClassCounts, "multi-stem");

            builder.AppendLine();
            builder.AppendLine("### Problem classes");
            builder.AppendLine();
            builder.AppendLine("| Class | Count |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| original-63 | {original63:N0} |");
            builder.AppendLine($"| 59-candidate | {candidate59:N0} |");
            builder.AppendLine($"| missing-recovery | {missingRecovery:N0} |");
            builder.AppendLine($"| uncertain | {uncertain:N0} |");
            builder.AppendLine($"| multi-stem | {multiStem:N0} |");

            if (summary.SpotChecks.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("| Spot check | Operation | Applied lemma |");
                builder.AppendLine("|---|---|---|");

                foreach (var spotCheck in summary.SpotChecks)
                {
                    builder.AppendLine(
                        $"| {spotCheck.Location} | {spotCheck.OperationKind} | {spotCheck.AppliedLemmaArabic ?? string.Empty} |");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine(
            "_Hard checks gate persistence; `warning` checks are informational and never change the verdict._");
        builder.AppendLine();
        builder.AppendLine("| Id | Severity | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|---|");

        if (report.Checks.Count == 0)
        {
            builder.AppendLine("| _none_ | | | | |");
        }
        else
        {
            foreach (var check in report.Checks)
            {
                var passed = check.Passed ? "✅" : "❌";
                builder.AppendLine(
                    $"| {check.Id} | {check.Severity} | {check.Expected} | {check.Observed} | {passed} |");
            }
        }

        if (report.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Warnings");
            builder.AppendLine();
            foreach (var warning in report.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        if (report.Errors.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Errors");
            builder.AppendLine();
            foreach (var error in report.Errors)
            {
                builder.AppendLine($"- {error}");
            }
        }

        if (report.InfoNotes.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Notes");
            builder.AppendLine();
            foreach (var note in report.InfoNotes)
            {
                builder.AppendLine($"- {note}");
            }
        }

        return builder.ToString();
    }

    private sealed record ReportDocument(
        DateTimeOffset RunAtUtc,
        string Verdict,
        bool Persisted,
        bool Forced,
        ReportTotals Totals,
        IReadOnlyList<ReportCheck> Checks,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> InfoNotes,
        WordLemmaCorrectionSummary? CorrectionSummary,
        ApprovedRootFallbackSummary? RootFallbackSummary);

    private sealed record ReportTotals(
        int MorphologyRows,
        int SegmentRows,
        int RootRows,
        int LemmaRows,
        int LemmaAnalysisRows,
        int StemRows,
        int PosTagRows,
        int ReadableWords,
        int EmptyFormRenders,
        int CleanCount,
        int QuranicMarksCount,
        int ReviewCount,
        int MultiwordCount);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);

    private static int GetProblemClassCount(IReadOnlyDictionary<string, int> counts, string key) =>
        counts.TryGetValue(key, out var value) ? value : 0;

    private static string? ExtractLeadingCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var token = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
