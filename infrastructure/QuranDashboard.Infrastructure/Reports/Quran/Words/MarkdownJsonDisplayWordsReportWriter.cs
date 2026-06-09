using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuranDashboard.Application.Abstractions.Quran.Words.Display;

namespace QuranDashboard.Infrastructure.Reports.Quran.Words;

public sealed class MarkdownJsonDisplayWordsReportWriter : IDisplayWordsReportWriter
{
    private const string MarkdownFileName = "words-display-report.md";
    private const string JsonFileName = "words-display-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(DisplayWordsRebuildResult result, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, JsonFileName);
        var markdownPath = Path.Combine(outputDir, MarkdownFileName);

        var reportDocument = new ReportDocument(
            result.RunAtUtc,
            result.Verdict,
            result.Persisted,
            result.Forced,
            new ReportTotals(
                result.Totals.OrderedTashkeelRows,
                result.Totals.OrderedSimpleRows,
                result.Totals.UniqueTashkeelRows,
                result.Totals.UniqueSimpleRows,
                result.Totals.ReadableWords),
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
        builder.AppendLine("# Quran Words Display Tables — Rebuild Report");
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
        builder.AppendLine($"| quran_words_ordered_tashkeel | {report.Totals.OrderedTashkeelRows:N0} |");
        builder.AppendLine($"| quran_words_ordered_simple | {report.Totals.OrderedSimpleRows:N0} |");
        builder.AppendLine($"| quran_words_unique_tashkeel | {report.Totals.UniqueTashkeelRows:N0} |");
        builder.AppendLine($"| quran_words_unique_simple | {report.Totals.UniqueSimpleRows:N0} |");
        builder.AppendLine($"| readable words (source) | {report.Totals.ReadableWords:N0} |");
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
        int OrderedTashkeelRows,
        int OrderedSimpleRows,
        int UniqueTashkeelRows,
        int UniqueSimpleRows,
        int ReadableWords);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
