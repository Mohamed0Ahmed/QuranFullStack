using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportReportFactory
{
    internal static AbwabSnapshotImportAuditReport BuildCandidate(
        DateTimeOffset runAtUtc,
        AbwabSnapshotSourcePackage package,
        string maskedTarget,
        IReadOnlyList<string> warnings) =>
        Build(
            runAtUtc,
            AbwabSnapshotImportContract.PendingVerdict,
            AbwabSnapshotImportContract.PersistedUnknown,
            package,
            maskedTarget,
            null,
            package.Snapshot.Counts,
            package.Checks,
            warnings,
            [],
            package.SourcePath);

    internal static AbwabSnapshotImportAuditReport BuildExecution(
        DateTimeOffset runAtUtc,
        AbwabSnapshotSourcePackage package,
        string maskedTarget,
        AbwabSnapshotImportExecution execution,
        IReadOnlyList<string> warnings) =>
        Build(
            runAtUtc,
            execution.Verdict,
            execution.Persisted,
            package,
            maskedTarget,
            execution.Result?.TargetMigrationHead,
            execution.Result?.Counts ?? package.Snapshot.Counts,
            package.Checks
                .Concat(execution.Result?.Checks ?? [])
                .Concat(execution.Checks)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            warnings.Concat(execution.Warnings).Distinct(StringComparer.Ordinal).ToArray(),
            execution.Errors,
            package.SourcePath);

    internal static async Task WriteFailureAsync(
        DateTimeOffset runAtUtc,
        string sourcePath,
        AbwabSnapshotSourcePackage? package,
        string? maskedTarget,
        string? targetMigrationHead,
        IReadOnlyList<string> checks,
        IReadOnlyList<string> warnings,
        string error,
        AbwabSnapshotImportReportPaths reportPaths)
    {
        var report = Build(
            runAtUtc,
            AbwabSnapshotImportContract.FailVerdict,
            AbwabSnapshotImportContract.PersistedFalse,
            package,
            maskedTarget,
            targetMigrationHead,
            package?.Snapshot.Counts ?? new Dictionary<string, AbwabSnapshotTableCount>(),
            checks,
            warnings,
            [error],
            sourcePath);
        try
        {
            await AbwabSnapshotImportReportWriter.WriteAsync(report, reportPaths, CancellationToken.None);
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("verdict=fail");
            Console.Error.WriteLine("persisted=false");
            Console.WriteLine($"report_json={reportPaths.Json}");
            Console.WriteLine($"report_markdown={reportPaths.Markdown}");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                $"Durable Abwab import failure report writing also failed ({exception.GetType().Name}).");
        }
    }

    private static AbwabSnapshotImportAuditReport Build(
        DateTimeOffset runAtUtc,
        string verdict,
        string persisted,
        AbwabSnapshotSourcePackage? package,
        string? maskedTarget,
        string? targetMigrationHead,
        IReadOnlyDictionary<string, AbwabSnapshotTableCount> counts,
        IReadOnlyList<string> checks,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        string fallbackSourcePath) =>
        new(
            AbwabSnapshotImportContract.Operation,
            verdict,
            persisted,
            runAtUtc,
            package?.SourcePath ?? fallbackSourcePath,
            package?.Sha256,
            maskedTarget,
            package?.Snapshot.Format,
            package?.Snapshot.FormatVersion,
            package?.Snapshot.Source.MigrationHead,
            targetMigrationHead,
            counts,
            package?.Snapshot.Scope.SourceExcludedRowCounts ?? new Dictionary<string, long>(),
            checks,
            warnings,
            errors);
}
