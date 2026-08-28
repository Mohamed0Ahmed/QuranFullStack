namespace QuranDashboard.Infrastructure.Reports.Quran.DataPipelines.PhraseSearch;

internal sealed class PhraseIndexBuildReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    internal async Task WriteAsync(
        PhraseIndexBuildReport report,
        string reportDirectory,
        CancellationToken ct)
    {
        Directory.CreateDirectory(reportDirectory);
        var jsonPath = Path.Combine(reportDirectory, "phrase-index-build-report.json");
        var markdownPath = Path.Combine(reportDirectory, "phrase-index-build-report.md");
        var jsonTemporaryPath = jsonPath + ".tmp";
        var markdownTemporaryPath = markdownPath + ".tmp";

        await using (var stream = new FileStream(
                         jsonTemporaryPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         4096,
                         FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, report, JsonOptions, ct);
            await stream.FlushAsync(ct);
            stream.Flush(flushToDisk: true);
        }

        await using (var stream = new FileStream(
                         markdownTemporaryPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         4096,
                         FileOptions.Asynchronous | FileOptions.WriteThrough))
        await using (var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            await writer.WriteAsync(BuildMarkdown(report).AsMemory(), ct);
            await writer.FlushAsync(ct);
            await stream.FlushAsync(ct);
            stream.Flush(flushToDisk: true);
        }
        File.Move(jsonTemporaryPath, jsonPath, overwrite: true);
        File.Move(markdownTemporaryPath, markdownPath, overwrite: true);
    }

    private static string BuildMarkdown(PhraseIndexBuildReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Quran Phrase Index — Build Report");
        builder.AppendLine();
        builder.AppendLine($"- Build ID: `{report.BuildId}`");
        builder.AppendLine($"- Status: {report.Status}");
        builder.AppendLine($"- Outcome: {report.Outcome}");
        builder.AppendLine($"- Format / builder: {report.FormatVersion} / {report.BuilderVersion}");
        builder.AppendLine(
            $"- Persisted / active: {FormatState(report.Persisted)} / {FormatState(report.Active)}");
        builder.AppendLine(
            $"- Exact / similarity ready: {FormatState(report.ExactReady)} / {FormatState(report.SimilarityReady)}");
        builder.AppendLine($"- Forced: {report.Forced}");
        builder.AppendLine($"- Started (UTC): {report.StartedAtUtc:u}");
        builder.AppendLine($"- Completed (UTC): {report.CompletedAtUtc:u}");
        builder.AppendLine($"- Duration: {report.DurationMilliseconds:N0} ms");
        builder.AppendLine($"- Peak managed memory: {report.PeakManagedMemoryBytes:N0} bytes");
        builder.AppendLine();
        builder.AppendLine("## Source fence");
        builder.AppendLine();
        builder.AppendLine($"- Before: revision {report.SourceRevisionBefore}, `{report.SourceFingerprintBefore}`");
        builder.AppendLine($"- At activation: revision {report.SourceRevisionAtActivation}, `{report.SourceFingerprintAtActivation}`");
        builder.AppendLine($"- Previous build: `{report.PreviousBuildId?.ToString() ?? "none"}`");
        builder.AppendLine($"- Active build: `{report.ActiveBuildId?.ToString() ?? "none"}`");
        builder.AppendLine();
        builder.AppendLine("## Totals");
        builder.AppendLine();
        builder.AppendLine("| Search tokens | Variants | Occurrences | Edges | Anchor stats |");
        builder.AppendLine("|---:|---:|---:|---:|---:|");
        builder.AppendLine(
            $"| {report.Totals.SearchTokens:N0} | {report.Totals.Variants:N0} | "
            + $"{report.Totals.Occurrences:N0} | {report.Totals.SimilarityEdges:N0} | "
            + $"{report.Totals.SimilarityAnchorStats:N0} |");
        builder.AppendLine();
        builder.AppendLine("## Disk preflight");
        builder.AppendLine();
        builder.AppendLine($"- Database: {report.DiskPreflight.DatabaseBytes:N0} bytes");
        builder.AppendLine($"- Existing phrase generations: {report.DiskPreflight.ExistingPhraseIndexBytes:N0} bytes");
        builder.AppendLine($"- Additional generation allowance: {report.DiskPreflight.AdditionalGenerationBytes:N0} bytes");
        builder.AppendLine($"- WAL headroom: {report.DiskPreflight.WalHeadroomBytes:N0} bytes");
        builder.AppendLine($"- Safety margin: {report.DiskPreflight.SafetyMarginBytes:N0} bytes");
        builder.AppendLine($"- Database filesystem available: {report.DiskPreflight.AvailableDatabaseFilesystemBytes:N0} bytes");
        builder.AppendLine($"- Required free space: {report.DiskPreflight.RequiredFreeBytes:N0} bytes");
        builder.AppendLine($"- Proof: {report.DiskPreflight.ProofKind} / verified={report.DiskPreflight.ProofVerified}");
        builder.AppendLine($"- Passed: {report.DiskPreflight.Passed}");
        builder.AppendLine();
        builder.AppendLine("## Per-mode and per-length metrics");
        builder.AppendLine();
        builder.AppendLine("| Mode | Words | Windows | Variants | Algorithm | Emissions | Candidates | Verified | Edges | Elapsed ms | Peak memory |");
        builder.AppendLine("|---:|---:|---:|---:|---|---:|---:|---:|---:|---:|---:|");
        foreach (var metric in report.Metrics.OrderBy(metric => metric.Mode).ThenBy(metric => metric.WordCount))
        {
            builder.AppendLine(
                $"| {metric.Mode} | {metric.WordCount} | {metric.RawWindows:N0} | {metric.Variants:N0} | "
                + $"{metric.Algorithm} | {metric.CandidateEmissions:N0} | {metric.UniqueCandidates:N0} | "
                + $"{metric.VerifiedPairs:N0} | {metric.Edges:N0} | {metric.ElapsedMilliseconds:N0} | "
                + $"{metric.PeakManagedMemoryBytes?.ToString("N0", CultureInfo.InvariantCulture) ?? "n/a"} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Checks");
        builder.AppendLine();
        builder.AppendLine("| ID | Severity | Expected | Observed | Passed |");
        builder.AppendLine("|---|---|---|---|---|");
        foreach (var check in report.Checks)
        {
            builder.AppendLine(
                $"| {check.Id} | {check.Severity} | {check.Expected} | {check.Observed} | {check.Passed} |");
        }

        AppendMessages(builder, "Warnings", report.Warnings);
        AppendMessages(builder, "Errors", report.Errors);
        return builder.ToString();
    }

    private static string FormatState(bool? state) => state?.ToString() ?? "unknown";

    private static void AppendMessages(
        StringBuilder builder,
        string heading,
        IReadOnlyList<string> messages)
    {
        if (messages.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        builder.AppendLine();
        foreach (var message in messages)
        {
            builder.AppendLine($"- {message}");
        }
    }
}
