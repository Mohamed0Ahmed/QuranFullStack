using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Tafsirs;

public sealed class MarkdownJsonTafsirReportWriter : ITafsirReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(TafsirImportReport report, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, TafsirImportConstants.JsonReportFileName);
        var markdownPath = Path.Combine(outputDir, TafsirImportConstants.MarkdownReportFileName);

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, BuildJsonDocument(report), JsonOptions, ct);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), Encoding.UTF8, ct);
    }

    private static object BuildJsonDocument(TafsirImportReport report) => new
    {
        runAtUtc = report.RunAtUtc,
        verdict = report.Verdict,
        persisted = report.Persisted,
        forced = report.Forced,
        profile = report.Profile,
        sourcePath = report.SourcePath,
        totals = new
        {
            sourceRows = report.Totals.SourceRows,
            tafsirTextBlockRows = report.Totals.TafsirTextBlockRows,
            ayahMappingRows = report.Totals.AyahMappingRows,
            approvedSources = report.Totals.ApprovedSources,
            excludedSources = report.Totals.ExcludedSources,
            arabicSources = report.Totals.ArabicSources,
            nonArabicSources = report.Totals.NonArabicSources,
            languageCount = report.Totals.LanguageCount,
            distinctAyahs = report.Totals.DistinctAyahs
        },
        sourceSummaries = report.SourceSummaries.Select(summary => new
        {
            sourceKey = summary.SourceKey,
            languageCode = summary.LanguageCode,
            direction = summary.Direction,
            displayNameEn = summary.DisplayNameEn,
            packageFile = summary.PackageFile,
            sha256 = summary.Sha256,
            license = summary.License,
            provenance = summary.Provenance
        }),
        excludedSourceSummaries = report.ExcludedSourceSummaries.Select(summary => new
        {
            sourceKey = summary.SourceKey,
            status = summary.Status,
            reason = summary.Reason
        }),
        checks = report.Checks.Select(check => new
        {
            id = check.Id,
            severity = check.Severity,
            expected = check.Expected,
            observed = check.Observed,
            passed = check.Passed
        }),
        warnings = report.Warnings,
        errors = report.Errors,
        infoNotes = report.InfoNotes
    };

    private static string BuildMarkdown(TafsirImportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Quran Tafsir Import Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunAtUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict.ToUpperInvariant()}");
        builder.AppendLine($"- Persisted: {report.Persisted.ToString().ToLowerInvariant()}");
        builder.AppendLine($"- Forced: {report.Forced.ToString().ToLowerInvariant()}");
        builder.AppendLine($"- Profile: {report.Profile}");
        builder.AppendLine($"- Source path: {report.SourcePath}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Sources | {report.Totals.SourceRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Text blocks | {report.Totals.TafsirTextBlockRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Source-to-ayah mappings | {report.Totals.AyahMappingRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Excluded sources | {report.Totals.ExcludedSources.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Languages | {report.Totals.LanguageCount.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine();
        AppendChecksSection(builder, report.Checks.Where(check => check.Severity == TafsirImportConstants.HardSeverity));

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

        if (report.ExcludedSourceSummaries.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Excluded Sources");
            builder.AppendLine();
            builder.AppendLine("| Source key | Status | Reason |");
            builder.AppendLine("|---|---|---|");
            foreach (var summary in report.ExcludedSourceSummaries)
            {
                builder.AppendLine($"| {summary.SourceKey} | {summary.Status} | {summary.Reason} |");
            }
        }

        return builder.ToString();
    }

    private static void AppendChecksSection(StringBuilder builder, IEnumerable<TafsirCheckResult> checks)
    {
        builder.AppendLine("## Hard Checks");
        builder.AppendLine();
        builder.AppendLine("| ID | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var check in checks)
        {
            builder.AppendLine(
                $"| {check.Id} | {check.Expected} | {check.Observed} | {FormatPassed(check.Passed)} |");
        }
    }

    private static string FormatPassed(bool passed) => passed ? "yes" : "no";
}
