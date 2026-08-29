using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal static class AbwabSnapshotArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static AbwabSnapshotArtifactPaths BuildPaths(string outputDirectory, DateTimeOffset exportedAtUtc)
    {
        var stamp = exportedAtUtc.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
        var baseName = $"abwab-snapshot-{stamp}";
        return new AbwabSnapshotArtifactPaths(
            Path.Combine(outputDirectory, $"{baseName}.json"),
            Path.Combine(outputDirectory, $"{baseName}.json.sha256"),
            Path.Combine(outputDirectory, $"{baseName}-report.json"),
            Path.Combine(outputDirectory, $"{baseName}-report.md"));
    }

    internal static async Task<(AbwabSnapshotAuditReport Report, AbwabSnapshotArtifactPaths Paths)> WriteAsync(
        AbwabSnapshotDocument snapshot,
        AbwabSnapshotValidationResult validation,
        string maskedTarget,
        AbwabSnapshotArtifactPaths paths,
        CancellationToken cancellationToken)
    {
        if (!validation.Succeeded)
        {
            throw new InvalidOperationException("A failed Abwab snapshot validation cannot be persisted.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(paths.Snapshot)!);
        RefuseOverwrite(paths);

        var snapshotBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        snapshotBytes = AppendNewline(snapshotBytes);
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(snapshotBytes));
        var checks = validation.Checks.Concat(["snapshot-written", "checksum-written", "audit-reports-written"]).ToArray();
        var report = new AbwabSnapshotAuditReport(
            "export",
            "pass",
            true,
            snapshot.ExportedAtUtc,
            maskedTarget,
            paths.Snapshot,
            sha256,
            snapshot.Format,
            snapshot.FormatVersion,
            snapshot.Counts,
            snapshot.Scope.SourceExcludedRowCounts,
            checks,
            validation.Warnings,
            validation.Errors);
        var reportJson = AppendNewline(JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions));
        var reportMarkdown = Encoding.UTF8.GetBytes(BuildMarkdown(report));
        var checksum = Encoding.UTF8.GetBytes($"{sha256}  {Path.GetFileName(paths.Snapshot)}{Environment.NewLine}");

        var artifacts = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [paths.Snapshot] = snapshotBytes,
            [paths.Checksum] = checksum,
            [paths.JsonReport] = reportJson,
            [paths.MarkdownReport] = reportMarkdown,
        };
        await WriteWithoutOverwriteAsync(artifacts, cancellationToken);
        return (report, paths);
    }

    internal static async Task<AbwabSnapshotAuditReport> WriteFailureAuditAsync(
        AbwabSnapshotDocument snapshot,
        AbwabSnapshotValidationResult validation,
        string maskedTarget,
        AbwabSnapshotArtifactPaths paths,
        CancellationToken cancellationToken)
    {
        if (validation.Succeeded)
        {
            throw new InvalidOperationException("A successful Abwab snapshot validation cannot produce a failure audit.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(paths.JsonReport)!);
        RefuseOverwrite([paths.JsonReport, paths.MarkdownReport]);

        var report = new AbwabSnapshotAuditReport(
            "export",
            "fail",
            false,
            snapshot.ExportedAtUtc,
            maskedTarget,
            null,
            null,
            snapshot.Format,
            snapshot.FormatVersion,
            snapshot.Counts,
            snapshot.Scope.SourceExcludedRowCounts,
            validation.Checks,
            validation.Warnings,
            validation.Errors);
        var artifacts = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [paths.JsonReport] = AppendNewline(JsonSerializer.SerializeToUtf8Bytes(report, JsonOptions)),
            [paths.MarkdownReport] = Encoding.UTF8.GetBytes(BuildMarkdown(report)),
        };
        await WriteWithoutOverwriteAsync(artifacts, cancellationToken);
        return report;
    }

    private static void RefuseOverwrite(AbwabSnapshotArtifactPaths paths)
    {
        RefuseOverwrite([paths.Snapshot, paths.Checksum, paths.JsonReport, paths.MarkdownReport]);
    }

    private static void RefuseOverwrite(IEnumerable<string> paths)
    {
        var existing = paths.FirstOrDefault(File.Exists);
        if (existing is not null)
        {
            throw new IOException($"Refusing to overwrite existing artifact: {existing}");
        }
    }

    private static async Task WriteWithoutOverwriteAsync(
        IReadOnlyDictionary<string, byte[]> artifacts,
        CancellationToken cancellationToken)
    {
        var temporaryPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        var persistedPaths = new List<string>();
        try
        {
            foreach (var artifact in artifacts)
            {
                var temporaryPath = $"{artifact.Key}.{Guid.NewGuid():N}.tmp";
                temporaryPaths.Add(artifact.Key, temporaryPath);
                await using var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
                await stream.WriteAsync(artifact.Value, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            foreach (var artifact in artifacts)
            {
                File.Move(temporaryPaths[artifact.Key], artifact.Key, false);
                persistedPaths.Add(artifact.Key);
            }
        }
        catch
        {
            foreach (var temporaryPath in temporaryPaths.Values.Where(File.Exists))
            {
                File.Delete(temporaryPath);
            }

            foreach (var persistedPath in persistedPaths.Where(File.Exists))
            {
                File.Delete(persistedPath);
            }

            throw;
        }
    }

    private static byte[] AppendNewline(byte[] bytes)
    {
        var result = new byte[bytes.Length + 1];
        bytes.CopyTo(result, 0);
        result[^1] = (byte)'\n';
        return result;
    }

    private static string BuildMarkdown(AbwabSnapshotAuditReport report)
    {
        var builder = new StringBuilder()
            .AppendLine("# Abwab Snapshot Export Report")
            .AppendLine()
            .AppendLine($"- Verdict: `{report.Verdict}`")
            .AppendLine($"- Persisted: `{report.Persisted.ToString().ToLowerInvariant()}`")
            .AppendLine($"- Target: `{report.MaskedTarget}`")
            .AppendLine($"- Snapshot: `{report.SnapshotPath ?? "not-written"}`")
            .AppendLine($"- SHA-256: `{report.SnapshotSha256 ?? "not-written"}`")
            .AppendLine($"- Format: `{report.Format}` v{report.FormatVersion}")
            .AppendLine()
            .AppendLine("## Counts")
            .AppendLine();
        foreach (var table in AbwabSnapshotContract.Tables)
        {
            builder.AppendLine($"- `{table}`: {report.Counts[table].Total}");
        }

        builder.AppendLine().AppendLine("## Source Rows Deliberately Excluded").AppendLine();
        foreach (var exclusion in report.SourceExcludedRowCounts.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            builder.AppendLine($"- `{exclusion.Key}`: {exclusion.Value}");
        }

        builder.AppendLine().AppendLine("## Checks").AppendLine();
        if (report.Checks.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var check in report.Checks)
            {
                builder.AppendLine($"- PASS `{check}`");
            }
        }

        builder.AppendLine().AppendLine("## Warnings").AppendLine();
        if (report.Warnings.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var warning in report.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine().AppendLine("## Errors").AppendLine();
        if (report.Errors.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (var error in report.Errors)
            {
                builder.AppendLine($"- {error}");
            }
        }

        return builder.ToString();
    }
}
