using QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportContract
{
    internal const string Operation = "import";
    internal const string PassVerdict = "pass";
    internal const string FailVerdict = "fail";
    internal const string PendingVerdict = "pending";
    internal const string PersistedTrue = "true";
    internal const string PersistedFalse = "false";
    internal const string PersistedUnknown = "unknown";
    internal const string ReportBaseName = "abwab-snapshot-import";
}

internal sealed record AbwabSnapshotSourcePackage(
    string SourcePath,
    string ChecksumPath,
    string Sha256,
    AbwabSnapshotFileDigest SourceDigest,
    AbwabSnapshotFileDigest ChecksumDigest,
    AbwabSnapshotDocument Snapshot,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings);

internal sealed record AbwabSnapshotFileDigest(long Length, string Sha256);

internal sealed record AbwabSnapshotSequenceState(long NextValue, bool IsCalled);

internal sealed record AbwabSnapshotImportDatabaseResult(
    string TargetMigrationHead,
    IReadOnlyDictionary<string, AbwabSnapshotTableCount> Counts,
    IReadOnlyList<string> Checks);

internal sealed record AbwabSnapshotImportExecution(
    string Verdict,
    string Persisted,
    AbwabSnapshotImportDatabaseResult? Result,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

internal sealed record AbwabSnapshotImportReportPaths(
    string Directory,
    string Json,
    string Markdown,
    string Reservation);

internal sealed record AbwabSnapshotImportReportReservation(
    AbwabSnapshotImportReportPaths Paths,
    string StagingDirectory,
    string StagingJson,
    string StagingMarkdown);

internal sealed record AbwabSnapshotImportAuditReport(
    string Operation,
    string Verdict,
    string Persisted,
    DateTimeOffset RunAtUtc,
    string SourcePath,
    string? SourceSha256,
    string? MaskedTarget,
    string? Format,
    int? FormatVersion,
    string? SourceMigrationHead,
    string? TargetMigrationHead,
    IReadOnlyDictionary<string, AbwabSnapshotTableCount> Counts,
    IReadOnlyDictionary<string, long> SourceExcludedRowCounts,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

internal sealed class AbwabSnapshotImportException(
    string message,
    IReadOnlyList<string>? checks = null,
    IReadOnlyList<string>? warnings = null) : Exception(message)
{
    internal IReadOnlyList<string> Checks { get; } = checks ?? [];

    internal IReadOnlyList<string> Warnings { get; } = warnings ?? [];
}
