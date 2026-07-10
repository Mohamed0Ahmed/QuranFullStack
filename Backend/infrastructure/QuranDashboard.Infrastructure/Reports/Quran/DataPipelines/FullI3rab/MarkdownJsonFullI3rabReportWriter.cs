using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.FullI3rab;

public sealed class MarkdownJsonFullI3rabReportWriter : IFullI3rabReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(FullI3rabImportReport report, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, FullI3rabImportConstants.JsonReportFileName);
        var markdownPath = Path.Combine(outputDir, FullI3rabImportConstants.MarkdownReportFileName);

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, BuildJsonDocument(report), JsonOptions, ct);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), Encoding.UTF8, ct);
    }

    private static object BuildJsonDocument(FullI3rabImportReport report) => new
    {
        runAtUtc = report.RunAtUtc,
        verdict = report.Verdict,
        persisted = report.Persisted,
        forced = report.Forced,
        sourcePath = report.SourcePath,
        licenseStatus = FullI3rabImportConstants.LicenseStatus,
        provenanceStatus = FullI3rabImportConstants.ProvenanceStatus,
        usageScope = FullI3rabImportConstants.UsageScope,
        provenanceWarning = FullI3rabImportConstants.ProvenanceWarning,
        totals = new
        {
            sourceRows = report.Totals.SourceRows,
            entryRows = report.Totals.EntryRows,
            ayahMappingRows = report.Totals.AyahMappingRows,
            distinctAyahs = report.Totals.DistinctAyahs
        },
        sourceSummaries = report.SourceSummaries.Select(summary => new
        {
            sourceKey = summary.SourceKey,
            displayNameEn = summary.DisplayNameEn,
            packageFile = summary.PackageFile,
            sha256 = summary.Sha256,
            license = summary.License,
            provenance = summary.Provenance,
            usageScope = summary.UsageScope,
            markupFormat = summary.MarkupFormat,
            hasQuranQuotationMarkup = summary.HasQuranQuotationMarkup
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

    private static string BuildMarkdown(FullI3rabImportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Quran Full I'rab Import Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunAtUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict.ToUpperInvariant()}");
        builder.AppendLine($"- Persisted: {report.Persisted.ToString().ToLowerInvariant()}");
        builder.AppendLine($"- Forced: {report.Forced.ToString().ToLowerInvariant()}");
        builder.AppendLine($"- Source path: {report.SourcePath}");
        builder.AppendLine();
        builder.AppendLine("## Provenance");
        builder.AppendLine();
        builder.AppendLine($"- licenseStatus: {FullI3rabImportConstants.LicenseStatus}");
        builder.AppendLine($"- provenanceStatus: {FullI3rabImportConstants.ProvenanceStatus}");
        builder.AppendLine($"- usageScope: {FullI3rabImportConstants.UsageScope}");
        builder.AppendLine($"- **{FullI3rabImportConstants.ProvenanceWarning}**");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Sources | {report.Totals.SourceRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine($"| Entries | {report.Totals.EntryRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Source-to-ayah mappings | {report.Totals.AyahMappingRows.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine(
            $"| Distinct ayahs | {report.Totals.DistinctAyahs.ToString("N0", CultureInfo.InvariantCulture)} |");
        builder.AppendLine();
        AppendChecksSection(builder, report.Checks.Where(check => check.Severity == FullI3rabImportConstants.HardSeverity));

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

        if (report.SourceSummaries.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Source Summaries");
            builder.AppendLine();
            builder.AppendLine("| Source key | Package file | License | Provenance | Usage scope | Markup |");
            builder.AppendLine("|---|---|---|---|---|---|");
            foreach (var summary in report.SourceSummaries)
            {
                builder.AppendLine(
                    $"| {summary.SourceKey} | {summary.PackageFile} | {summary.License} | " +
                    $"{summary.Provenance} | {summary.UsageScope} | {summary.MarkupFormat} |");
            }
        }

        return builder.ToString();
    }

    private static void AppendChecksSection(StringBuilder builder, IEnumerable<FullI3rabCheckResult> checks)
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
