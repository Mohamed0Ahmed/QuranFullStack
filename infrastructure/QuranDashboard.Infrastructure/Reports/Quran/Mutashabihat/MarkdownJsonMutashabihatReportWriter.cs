using System.Text;
using System.Text.Json.Serialization;
using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Infrastructure.Reports.Quran.Mutashabihat;

public sealed class MarkdownJsonMutashabihatReportWriter : IMutashabihatReportWriter
{
    private const string MarkdownFileName = "mutashabihat-import-report.md";
    private const string JsonFileName = "mutashabihat-import-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(MutashabihatImportResult result, string outputDir, CancellationToken ct)
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
                result.Totals.GroupRows,
                result.Totals.RawOccurrenceEntries,
                result.Totals.StoredOccurrenceRows,
                result.Totals.LinkRows,
                result.Totals.DistinctSimilarSources,
                result.Totals.DistinctReferencedAyahs),
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
        builder.AppendLine("# Quran Mutashabihat — Import Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunAtUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict.ToUpperInvariant()}");
        builder.AppendLine($"- Persisted: {report.Persisted}");
        builder.AppendLine($"- Forced: {report.Forced}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| quran_mutashabihat_groups | {report.Totals.GroupRows:N0} |");
        builder.AppendLine(
            $"| quran_mutashabihat_occurrences | {report.Totals.StoredOccurrenceRows:N0} (stored unique) |");
        builder.AppendLine($"| raw source occurrence entries | {report.Totals.RawOccurrenceEntries:N0} |");
        builder.AppendLine($"| quran_similar_ayah_links | {report.Totals.LinkRows:N0} |");
        builder.AppendLine($"| distinct similar-ayah sources | {report.Totals.DistinctSimilarSources:N0} |");
        builder.AppendLine($"| distinct referenced ayahs | {report.Totals.DistinctReferencedAyahs:N0} |");
        builder.AppendLine();
        AppendChecksSection(builder, "Hard checks", report.Checks.Where(check => check.Severity == "hard"));
        AppendWarningTable(builder, report.Checks.Where(check => check.Severity == "warning"));
        AppendInformationalSection(builder, report.Checks.Where(check => check.Severity == "info"));

        if (report.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Warning notes");
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

    private static void AppendChecksSection(
        StringBuilder builder,
        string title,
        IEnumerable<ReportCheck> checks)
    {
        builder.AppendLine($"## {title}");
        builder.AppendLine();
        builder.AppendLine("| Id | Severity | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|---|");

        var checkList = checks.ToList();
        if (checkList.Count == 0)
        {
            builder.AppendLine("| _none_ | | | | |");
            return;
        }

        foreach (var check in checkList)
        {
            var passed = check.Passed ? "✅" : "❌";
            builder.AppendLine(
                $"| {check.Id} | {check.Severity} | {check.Expected} | {check.Observed} | {passed} |");
        }
    }

    private static void AppendWarningTable(StringBuilder builder, IEnumerable<ReportCheck> warnings)
    {
        builder.AppendLine();
        builder.AppendLine("## Warnings (recorded, never block)");
        builder.AppendLine();
        builder.AppendLine("| Id | Count | Note |");
        builder.AppendLine("|---|---:|---|");

        var warningList = warnings.ToList();
        if (warningList.Count == 0)
        {
            builder.AppendLine("| _none_ | 0 | |");
            return;
        }

        foreach (var warning in warningList)
        {
            builder.AppendLine($"| {warning.Id} | {warning.Observed} | recorded |");
        }
    }

    private static void AppendInformationalSection(StringBuilder builder, IEnumerable<ReportCheck> infoChecks)
    {
        builder.AppendLine();
        builder.AppendLine("## Informational");
        builder.AppendLine();

        var infoList = infoChecks.ToList();
        if (infoList.Count == 0)
        {
            builder.AppendLine("- _none_");
            return;
        }

        foreach (var info in infoList)
        {
            builder.AppendLine($"- {info.Id}: {info.Observed}");
        }
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
        int GroupRows,
        int RawOccurrenceEntries,
        int StoredOccurrenceRows,
        int LinkRows,
        int DistinctSimilarSources,
        int DistinctReferencedAyahs);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
