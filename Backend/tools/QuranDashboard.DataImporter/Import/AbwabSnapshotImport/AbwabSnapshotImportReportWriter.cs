using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static AbwabSnapshotImportReportPaths BuildPaths(
        string reportDirectory,
        DateTimeOffset runAtUtc)
    {
        var stamp = runAtUtc.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var baseName = $"{AbwabSnapshotImportContract.ReportBaseName}-{stamp}";
        var finalDirectory = Path.Combine(reportDirectory, baseName);
        return new AbwabSnapshotImportReportPaths(
            finalDirectory,
            Path.Combine(finalDirectory, "report.json"),
            Path.Combine(finalDirectory, "report.md"),
            $"{finalDirectory}.reserve");
    }

    internal static async Task WriteAsync(
        AbwabSnapshotImportAuditReport report,
        AbwabSnapshotImportReportPaths paths,
        CancellationToken cancellationToken)
    {
        var reservation = await ReserveAsync(report, paths, cancellationToken);
        await FinalizeAsync(reservation, report, cancellationToken);
    }

    internal static async Task<AbwabSnapshotImportReportReservation> ReserveAsync(
        AbwabSnapshotImportAuditReport candidate,
        AbwabSnapshotImportReportPaths paths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(paths);

        var parentDirectory = Path.GetDirectoryName(paths.Directory)!;
        Directory.CreateDirectory(parentDirectory);
        if (Directory.Exists(paths.Directory) || File.Exists(paths.Reservation))
        {
            throw new IOException($"Refusing to overwrite or collide with Abwab import report: {paths.Directory}");
        }

        var stagingDirectory = $"{paths.Directory}.pending-{Guid.NewGuid():N}";
        var reservation = new AbwabSnapshotImportReportReservation(
            paths,
            stagingDirectory,
            Path.Combine(stagingDirectory, "report.json"),
            Path.Combine(stagingDirectory, "report.md"));
        try
        {
            await using (var stream = new FileStream(
                paths.Reservation,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("reserved\n"), cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            Directory.CreateDirectory(stagingDirectory);
            await WritePairAsync(
                reservation.StagingJson,
                reservation.StagingMarkdown,
                candidate,
                cancellationToken);
            return reservation;
        }
        catch
        {
            if (Directory.Exists(stagingDirectory))
            {
                Directory.Delete(stagingDirectory, true);
            }

            if (File.Exists(paths.Reservation))
            {
                File.Delete(paths.Reservation);
            }

            throw;
        }
    }

    internal static async Task FinalizeAsync(
        AbwabSnapshotImportReportReservation reservation,
        AbwabSnapshotImportAuditReport report,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(report);

        if (Directory.Exists(reservation.Paths.Directory))
        {
            throw new IOException($"Refusing to overwrite existing Abwab import report: {reservation.Paths.Directory}");
        }

        var finalizingDirectory = $"{reservation.Paths.Directory}.final-{Guid.NewGuid():N}";
        try
        {
            Directory.CreateDirectory(finalizingDirectory);
            await WritePairAsync(
                Path.Combine(finalizingDirectory, "report.json"),
                Path.Combine(finalizingDirectory, "report.md"),
                report,
                cancellationToken);
            Directory.Move(finalizingDirectory, reservation.Paths.Directory);
        }
        catch
        {
            TryDeleteDirectory(finalizingDirectory);
            throw;
        }

        TryDeleteDirectory(reservation.StagingDirectory);
        TryDeleteFile(reservation.Paths.Reservation);
    }

    private static async Task WritePairAsync(
        string jsonPath,
        string markdownPath,
        AbwabSnapshotImportAuditReport report,
        CancellationToken cancellationToken)
    {
        var jsonBytes = AppendNewline(JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions));
        var markdownBytes = Encoding.UTF8.GetBytes(BuildMarkdown(report));
        await WriteOneAsync(jsonPath, jsonBytes, cancellationToken);
        await WriteOneAsync(markdownPath, markdownBytes, cancellationToken);
    }

    private static async Task WriteOneAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string BuildMarkdown(AbwabSnapshotImportAuditReport report)
    {
        var builder = new StringBuilder()
            .AppendLine("# Abwab Snapshot Import Report")
            .AppendLine()
            .AppendLine($"- Verdict: `{report.Verdict}`")
            .AppendLine($"- Persisted: `{report.Persisted}`")
            .AppendLine($"- Run (UTC): `{report.RunAtUtc:u}`")
            .AppendLine($"- Source: `{report.SourcePath}`")
            .AppendLine($"- Source SHA-256: `{report.SourceSha256 ?? "unavailable"}`")
            .AppendLine($"- Target: `{report.MaskedTarget ?? "not-opened"}`")
            .AppendLine($"- Format: `{report.Format ?? "unavailable"}` v{report.FormatVersion?.ToString(CultureInfo.InvariantCulture) ?? "?"}")
            .AppendLine($"- Source migration head: `{report.SourceMigrationHead ?? "unavailable"}`")
            .AppendLine($"- Target migration head: `{report.TargetMigrationHead ?? "not-read"}`")
            .AppendLine()
            .AppendLine("## Counts")
            .AppendLine();
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            if (report.Counts.TryGetValue(table, out var count))
            {
                builder.AppendLine(
                    $"- `{table}`: total={count.Total}, active={FormatNullable(count.Active)}, archived={FormatNullable(count.Archived)}");
            }
            else
            {
                builder.AppendLine($"- `{table}`: unavailable");
            }
        }

        builder.AppendLine().AppendLine("## Source Rows Deliberately Excluded").AppendLine();
        if (report.SourceExcludedRowCounts.Count == 0)
        {
            builder.AppendLine("- Unavailable");
        }
        else
        {
            foreach (var item in report.SourceExcludedRowCounts.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                builder.AppendLine($"- `{item.Key}`: {item.Value}");
            }
        }

        AppendList(builder, "Checks", report.Checks, "PASS");
        AppendList(builder, "Warnings", report.Warnings, null);
        AppendList(builder, "Errors", report.Errors, null);
        return builder.ToString();
    }

    private static void AppendList(
        StringBuilder builder,
        string heading,
        IReadOnlyList<string> values,
        string? prefix)
    {
        builder.AppendLine().AppendLine($"## {heading}").AppendLine();
        if (values.Count == 0)
        {
            builder.AppendLine("- None");
            return;
        }

        foreach (var value in values)
        {
            builder.AppendLine(prefix is null ? $"- {value}" : $"- {prefix} `{value}`");
        }
    }

    private static string FormatNullable(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "n/a";

    private static byte[] AppendNewline(byte[] bytes)
    {
        var result = new byte[bytes.Length + 1];
        bytes.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }
}
