using QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

namespace QuranDashboard.Infrastructure.Reports.Quran.Morphology;

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
            result.InfoNotes.ToList());

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
        IReadOnlyList<string> InfoNotes);

    private sealed record ReportTotals(
        int MorphologyRows,
        int SegmentRows,
        int RootRows,
        int LemmaRows,
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
}
