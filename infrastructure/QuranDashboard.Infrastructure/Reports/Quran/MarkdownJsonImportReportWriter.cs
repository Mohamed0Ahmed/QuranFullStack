using System.Text;
using System.Text.Json.Serialization;
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Infrastructure.Reports.Quran;

public sealed class MarkdownJsonImportReportWriter : IImportReportWriter
{
    private const string MarkdownFileName = "quran-foundation-import-report.md";
    private const string JsonFileName = "quran-foundation-import-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(QuranImportValidationResult result, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, JsonFileName);
        var markdownPath = Path.Combine(outputDir, MarkdownFileName);

        var reportDocument = new ReportDocument(
            result.RunAtUtc,
            result.SourceRoot,
            result.ManifestVersion,
            result.Verdict,
            result.Persisted,
            result.Forced,
            new ReportTotals(
                result.Totals.Surahs,
                result.Totals.Ayahs,
                result.Totals.Pages,
                result.Totals.Lines,
                result.Totals.Words,
                result.Totals.AyahMarkers,
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
        builder.AppendLine("# Quran Foundation Import Validation Report");
        builder.AppendLine();
        builder.AppendLine($"**Run at:** {report.RunAt:u}");
        builder.AppendLine($"**Source root:** `{report.SourceRoot}`");
        builder.AppendLine($"**Manifest version:** {report.ManifestVersion}");
        builder.AppendLine($"**Verdict:** `{report.Verdict}`");
        builder.AppendLine($"**Persisted:** {report.Persisted}");
        builder.AppendLine($"**Forced:** {report.Forced}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Dataset | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Surahs | {report.Totals.Surahs} |");
        builder.AppendLine($"| Ayahs | {report.Totals.Ayahs} |");
        builder.AppendLine($"| Pages | {report.Totals.Pages} |");
        builder.AppendLine($"| Lines | {report.Totals.Lines} |");
        builder.AppendLine($"| Words | {report.Totals.Words} |");
        builder.AppendLine($"| Ayah markers | {report.Totals.AyahMarkers} |");
        builder.AppendLine($"| Readable words | {report.Totals.ReadableWords} |");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine("| Id | Severity | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|---|");

        foreach (var check in report.Checks)
        {
            builder.AppendLine(
                $"| {check.Id} | {check.Severity} | {check.Expected} | {check.Observed} | {check.Passed} |");
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
            builder.AppendLine("## Informational Notes");
            builder.AppendLine();
            foreach (var note in report.InfoNotes)
            {
                builder.AppendLine($"- {note}");
            }
        }

        return builder.ToString();
    }

    private sealed record ReportDocument(
        DateTimeOffset RunAt,
        string SourceRoot,
        string ManifestVersion,
        string Verdict,
        bool Persisted,
        bool Forced,
        ReportTotals Totals,
        IReadOnlyList<ReportCheck> Checks,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> InfoNotes);

    private sealed record ReportTotals(
        int Surahs,
        int Ayahs,
        int Pages,
        int Lines,
        int Words,
        int AyahMarkers,
        int ReadableWords);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);
}
