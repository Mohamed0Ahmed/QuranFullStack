using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

namespace QuranDashboard.Infrastructure.Reports.Quran.Irab;

public sealed class MarkdownJsonI3rabReportWriter(QuranDashboardDbContext dbContext) : II3rabGenerationReportWriter
{
    private const string MarkdownFileName = "simple-i3rab-generation-report.md";
    private const string JsonFileName = "simple-i3rab-generation-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string Write(I3rabGenerationResult result, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var verdict = result.Checks.All(check => check.Passed) && result.Persisted ? "PASS" : "FAIL";
        var ruleUsage = result.Persisted ? ReadRuleUsage() : [];
        var document = new ReportDocument(
            DateTimeOffset.UtcNow,
            verdict,
            result.Persisted,
            result.Forced,
            new ReportTotals(
                result.SegmentCount,
                result.ReadableWordCount,
                result.RuleCount,
                result.FamilyCount,
                result.ApprovedCount,
                result.NeedsReviewCount,
                result.UnsupportedCount,
                result.WordsDisplayable,
                result.NullFormsPreserved),
            result.Checks
                .Select(check => new ReportCheck(check.Id, check.Severity, check.Expected, check.Observed, check.Passed))
                .ToList(),
            ruleUsage,
            result.Warnings
                .Select(warning => new ReportWarning(warning.Id, warning.Note))
                .ToList());

        var jsonPath = Path.Combine(outputDirectory, JsonFileName);
        var markdownPath = Path.Combine(outputDirectory, MarkdownFileName);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(document, JsonOptions), Encoding.UTF8);
        File.WriteAllText(markdownPath, BuildMarkdown(document), Encoding.UTF8);

        return markdownPath;
    }

    public string WriteRefusal(I3rabRefusalReport refusal, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(refusal);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        var document = new RefusalReportDocument(
            DateTimeOffset.UtcNow,
            "REFUSED",
            false,
            refusal.Forced,
            refusal.RefusalReason,
            new RefusalReadiness(
                refusal.Readiness.SegmentCount,
                refusal.Readiness.ReadableWordCount,
                refusal.Readiness.NullFormCount,
                refusal.Readiness.IsReady));

        var jsonPath = Path.Combine(outputDirectory, JsonFileName);
        var markdownPath = Path.Combine(outputDirectory, MarkdownFileName);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(document, JsonOptions), Encoding.UTF8);
        File.WriteAllText(markdownPath, BuildRefusalMarkdown(document), Encoding.UTF8);

        return markdownPath;
    }

    private static string BuildRefusalMarkdown(RefusalReportDocument report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Word Simple I‘rab — Generation Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict}");
        builder.AppendLine($"- Persisted: {report.Persisted}");
        builder.AppendLine($"- Forced: {report.Forced}");
        builder.AppendLine();
        builder.AppendLine("## Refusal");
        builder.AppendLine();
        builder.AppendLine(report.RefusalReason);
        builder.AppendLine();
        builder.AppendLine("## Readiness");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| segments | {report.Readiness.SegmentCount:N0} |");
        builder.AppendLine($"| readable_words | {report.Readiness.ReadableWordCount:N0} |");
        builder.AppendLine($"| null_forms | {report.Readiness.NullFormCount:N0} |");
        builder.AppendLine($"| ready | {report.Readiness.IsReady} |");
        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("Generation refused before any database writes.");

        return builder.ToString();
    }

    private IReadOnlyList<ReportRuleUsage> ReadRuleUsage()
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            return [];
        }

        using var command = new NpgsqlCommand(I3rabSql.RuleUsage, npgsqlConnection);
        using var reader = command.ExecuteReader();

        var rows = new List<ReportRuleUsage>();
        while (reader.Read())
        {
            rows.Add(new ReportRuleUsage(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return rows;
    }

    private static string BuildMarkdown(ReportDocument report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Word Simple I‘rab — Generation Report");
        builder.AppendLine();
        builder.AppendLine($"- Run (UTC): {report.RunUtc:u}");
        builder.AppendLine($"- Verdict: {report.Verdict}");
        builder.AppendLine($"- Persisted: {report.Persisted}");
        builder.AppendLine($"- Forced: {report.Forced}");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Metric | Value |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| segments | {report.Totals.Segments:N0} |");
        builder.AppendLine($"| words | {report.Totals.Words:N0} |");
        builder.AppendLine($"| rules | {report.Totals.Rules:N0} |");
        builder.AppendLine($"| families | {report.Totals.Families:N0} |");
        builder.AppendLine($"| approved | {report.Totals.Approved:N0} |");
        builder.AppendLine($"| needs_review | {report.Totals.NeedsReview:N0} |");
        builder.AppendLine($"| unsupported | {report.Totals.Unsupported:N0} |");
        builder.AppendLine($"| words_displayable | {report.Totals.WordsDisplayable:N0} |");
        builder.AppendLine($"| null_forms_preserved | {report.Totals.NullFormsPreserved:N0} |");
        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine("| id | severity | expected | observed | passed |");
        builder.AppendLine("|---|---|---|---|---|");
        foreach (var check in report.Checks)
        {
            builder.AppendLine(
                $"| {check.Id} | {check.Severity} | {check.Expected} | {check.Observed} | {check.Passed} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Rule usage");
        builder.AppendLine();
        if (report.RuleUsage.Count == 0)
        {
            builder.AppendLine("_No committed rule usage (rolled back or not persisted)._");
        }
        else
        {
            builder.AppendLine("| signature_key | rule_family | segments |");
            builder.AppendLine("|---|---|---:|");
            foreach (var row in report.RuleUsage.Where(row => row.Segments > 0).Take(25))
            {
                builder.AppendLine($"| {row.SignatureKey} | {row.RuleFamily} | {row.Segments:N0} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Warnings");
        builder.AppendLine();
        foreach (var warning in report.Warnings)
        {
            builder.AppendLine($"- **{warning.Id}**: {warning.Note}");
        }

        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine(
            report.Persisted
                ? "Generation committed after all hard checks passed. Labels are simplified, not authoritative scholarly i‘rab."
                : "Generation rolled back. No i‘rab changes were committed.");

        return builder.ToString();
    }

    private sealed record ReportDocument(
        DateTimeOffset RunUtc,
        string Verdict,
        bool Persisted,
        bool Forced,
        ReportTotals Totals,
        IReadOnlyList<ReportCheck> Checks,
        IReadOnlyList<ReportRuleUsage> RuleUsage,
        IReadOnlyList<ReportWarning> Warnings);

    private sealed record ReportTotals(
        int Segments,
        int Words,
        int Rules,
        int Families,
        int Approved,
        int NeedsReview,
        int Unsupported,
        int WordsDisplayable,
        int NullFormsPreserved);

    private sealed record ReportCheck(
        string Id,
        string Severity,
        string Expected,
        string Observed,
        bool Passed);

    private sealed record ReportRuleUsage(
        string SignatureKey,
        string RuleFamily,
        string I3rabArabic,
        int Segments);

    private sealed record ReportWarning(string Id, string Note);

    private sealed record RefusalReportDocument(
        DateTimeOffset RunUtc,
        string Verdict,
        bool Persisted,
        bool Forced,
        string RefusalReason,
        RefusalReadiness Readiness);

    private sealed record RefusalReadiness(
        int SegmentCount,
        int ReadableWordCount,
        int NullFormCount,
        bool IsReady);
}
