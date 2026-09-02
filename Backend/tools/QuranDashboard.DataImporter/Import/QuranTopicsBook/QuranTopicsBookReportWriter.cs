using System.Text;
using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal static class QuranTopicsBookReportWriter
{
    internal static async Task<(string Json, string Markdown)> WriteAsync(
        string reportDirectory,
        QuranTopicsBookAuditReport report,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(reportDirectory);
        var stamp = report.RunAtUtc.ToString("yyyyMMdd'T'HHmmssfff'Z'");
        var jsonPath = Path.Combine(reportDirectory, $"quran-topics-book-import-{stamp}.json");
        var markdownPath = Path.Combine(reportDirectory, $"quran-topics-book-import-{stamp}.md");
        if (File.Exists(jsonPath) || File.Exists(markdownPath))
        {
            throw new IOException("Refusing to overwrite an existing Quran topics book import report.");
        }

        var json = JsonSerializer.Serialize(report, QuranTopicsBookJsonContext.Default.QuranTopicsBookAuditReport);
        await File.WriteAllTextAsync(jsonPath, json + Environment.NewLine, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(markdownPath, BuildMarkdown(report), Encoding.UTF8, cancellationToken);
        return (jsonPath, markdownPath);
    }

    private static string BuildMarkdown(QuranTopicsBookAuditReport report)
    {
        var lines = new List<string>
        {
            "# Quran Topics Book Import Report",
            string.Empty,
            $"- Run: `{report.RunAtUtc:O}`",
            $"- Source: `{report.SourcePath}`",
            $"- Source SHA-256: `{report.SourceSha256}`",
            $"- Target: `{report.Target ?? "unavailable"}`",
            $"- Actor user ID: `{report.ActorUserId}`",
            $"- Validate only: `{report.ValidateOnly.ToString().ToLowerInvariant()}`",
            $"- Verdict: `{report.Verdict}`",
            $"- Persisted: `{report.Persisted}`",
        };
        if (report.Metrics is not null)
        {
            lines.AddRange([
                string.Empty,
                "## Totals",
                string.Empty,
                $"- Sections: `{report.Metrics.SectionCount}`",
                $"- Doors: `{report.Metrics.DoorCount}`",
                $"- Parent doors: `{report.Metrics.ParentDoorCount}`",
                $"- Leaf doors: `{report.Metrics.LeafDoorCount}`",
                $"- Ayah groups: `{report.Metrics.AyahGroupCount}`",
                $"- Grouped ranges: `{report.Metrics.GroupedRangeCount}`",
                $"- Ayah references: `{report.Metrics.AyahReferenceCount}`",
                $"- Unique verse keys: `{report.Metrics.UniqueVerseKeyCount}`",
            ]);
        }

        AddList(lines, "Checks", report.Checks);
        AddList(lines, "Warnings", report.Warnings);
        AddList(lines, "Errors", report.Errors);
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static void AddList(List<string> lines, string title, IReadOnlyList<string> values)
    {
        lines.Add(string.Empty);
        lines.Add($"## {title}");
        lines.Add(string.Empty);
        if (values.Count == 0)
        {
            lines.Add("- None");
            return;
        }

        lines.AddRange(values.Select(value => $"- {value}"));
    }
}
