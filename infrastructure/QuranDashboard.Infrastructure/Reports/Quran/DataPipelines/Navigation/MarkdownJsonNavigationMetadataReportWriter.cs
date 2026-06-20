using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.Navigation;

public sealed class MarkdownJsonNavigationMetadataReportWriter : INavigationMetadataReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task WriteAsync(NavigationMetadataImportReport report, string reportOutDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportOutDir);

        Directory.CreateDirectory(reportOutDir);

        var jsonPath = Path.Combine(reportOutDir, NavigationImportConstants.JsonReportFileName);
        var markdownPath = Path.Combine(reportOutDir, NavigationImportConstants.MarkdownReportFileName);

        await using (var jsonStream = File.Create(jsonPath))
        {
            await JsonSerializer.SerializeAsync(jsonStream, BuildJsonDocument(report), JsonOptions, ct);
        }

        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), Encoding.UTF8, ct);
    }

    private static object BuildJsonDocument(NavigationMetadataImportReport report) => new
    {
        feature = report.Feature,
        verdict = report.Verdict,
        persisted = report.Persisted,
        forced = report.Forced,
        runAtUtc = report.RunAtUtc,
        sourcePath = report.SourcePath,
        manifest = new
        {
            packageType = report.Manifest.PackageType,
            isFinalImportManifest = report.Manifest.IsFinalImportManifest
        },
        totals = new
        {
            juz = report.Totals.Juz,
            hizb = report.Totals.Hizb,
            rub = report.Totals.Rub,
            sajda = report.Totals.Sajda,
            ayahsTagged = report.Totals.AyahsTagged
        },
        ayahCoverage = new
        {
            totalAyahs = report.AyahCoverage.TotalAyahs,
            withJuz = report.AyahCoverage.WithJuz,
            withHizb = report.AyahCoverage.WithHizb,
            withRub = report.AyahCoverage.WithRub,
            complete = report.AyahCoverage.Complete
        },
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
        noQuranAyahTextReadOrStored = report.NoQuranAyahTextReadOrStored
    };

    private static string BuildMarkdown(NavigationMetadataImportReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Quran Navigation Metadata Import Report");
        builder.AppendLine();
        builder.AppendLine($"Verdict: **{report.Verdict}**");
        builder.AppendLine($"Persisted: {report.Persisted.ToString().ToLowerInvariant()}");
        builder.AppendLine($"Forced: {report.Forced.ToString().ToLowerInvariant()}");
        builder.AppendLine($"Source path: `{report.SourcePath}`");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Dataset | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| juz | {report.Totals.Juz} |");
        builder.AppendLine($"| hizb | {report.Totals.Hizb} |");
        builder.AppendLine($"| rub | {report.Totals.Rub} |");
        builder.AppendLine($"| sajda | {report.Totals.Sajda} |");
        builder.AppendLine($"| ayahsTagged | {report.Totals.AyahsTagged} |");
        builder.AppendLine();
        builder.AppendLine("## Ayah coverage");
        builder.AppendLine();
        builder.AppendLine($"Total ayahs: {report.AyahCoverage.TotalAyahs}");
        builder.AppendLine($"With juz: {report.AyahCoverage.WithJuz}");
        builder.AppendLine($"With hizb: {report.AyahCoverage.WithHizb}");
        builder.AppendLine($"With rub: {report.AyahCoverage.WithRub}");
        builder.AppendLine($"Complete: {report.AyahCoverage.Complete}");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        foreach (var check in report.Checks)
        {
            builder.AppendLine($"- `{check.Id}`: {(check.Passed ? "PASS" : "FAIL")} (expected {check.Expected}; observed {check.Observed})");
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

        builder.AppendLine();
        builder.AppendLine("No Quran ayah text was read or stored by this import.");
        return builder.ToString();
    }
}
