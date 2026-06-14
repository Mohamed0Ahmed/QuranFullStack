using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Reports.Quran.Tafsirs;

/// <summary>
/// Phase 3 minimal report writer for US1 acceptance. Phase 4 (T056) enhances report detail.
/// </summary>
public sealed class MarkdownJsonTafsirReportWriter : ITafsirReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(TafsirImportResult result, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, TafsirImportConstants.JsonReportFileName);
        var markdownPath = Path.Combine(outputDir, TafsirImportConstants.MarkdownReportFileName);

        var reportDocument = new ReportDocument(
            result.RunAtUtc,
            result.Verdict,
            result.Persisted,
            result.Forced,
            new ReportTotals(
                result.Totals.SourceRows,
                result.Totals.TafsirTextBlockRows,
                result.Totals.AyahMappingRows,
                result.Totals.ApprovedSources,
                result.Totals.ExcludedSources,
                result.Totals.ArabicSources,
                result.Totals.NonArabicSources,
                result.Totals.LanguageCount,
                result.Totals.DistinctAyahs),
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
        builder.AppendLine("# Quran Tafsir — Import Report");
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
        builder.AppendLine($"| quran_tafsir_sources | {report.Totals.SourceRows:N0} |");
        builder.AppendLine($"| quran_tafsir_entries | {report.Totals.TafsirTextBlockRows:N0} |");
        builder.AppendLine($"| quran_tafsir_ayah_entries | {report.Totals.AyahMappingRows:N0} |");
        builder.AppendLine($"| distinct ayahs | {report.Totals.DistinctAyahs:N0} |");
        builder.AppendLine();
        AppendChecksSection(builder, report.Checks.Where(check => check.Severity == "hard"));

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

        return builder.ToString();
    }

    private static void AppendChecksSection(StringBuilder builder, IEnumerable<ReportCheck> checks)
    {
        builder.AppendLine("## Hard checks");
        builder.AppendLine();
        builder.AppendLine("| ID | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var check in checks)
        {
            builder.AppendLine(
                $"| {check.Id} | {check.Expected} | {check.Observed} | {check.Passed} |");
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
        int SourceRows,
        long TafsirTextBlockRows,
        long AyahMappingRows,
        int ApprovedSources,
        int ExcludedSources,
        int ArabicSources,
        int NonArabicSources,
        int LanguageCount,
        int DistinctAyahs);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
