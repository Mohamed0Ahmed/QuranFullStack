using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal static class AbwabSnapshotContract
{
    internal const string Format = "quran-dashboard-abwab-snapshot";
    internal const int FormatVersion = 4;
    internal const string ExcludedDerivedRowsTable = "abwab_door_inclusion_unit_syncs";

    internal static readonly string[] Tables =
    [
        "abwab_sections",
        "abwab_doors",
        "abwab_door_aliases",
        "abwab_door_relations",
        "abwab_templates",
        "abwab_template_nodes",
        "abwab_door_inclusions",
        "abwab_door_inclusion_unit_syncs",
    ];
}

internal sealed record AbwabSnapshotDocument(
    string Format,
    int FormatVersion,
    DateTimeOffset ExportedAtUtc,
    AbwabSnapshotSource Source,
    AbwabSnapshotScope Scope,
    AbwabSnapshotRestorePolicy RestorePolicy,
    IReadOnlyList<AbwabSnapshotSchemaColumn> SchemaColumns,
    IReadOnlyDictionary<string, AbwabSnapshotTableCount> Counts,
    IReadOnlyDictionary<string, IReadOnlyList<JsonElement>> Tables);

internal sealed record AbwabSnapshotSource(
    string Database,
    string ServerVersion,
    string MigrationHead,
    bool TransactionReadOnly);

internal sealed record AbwabSnapshotScope(
    IReadOnlyList<string> AbwabTables,
    bool LinkingRowsIncluded,
    bool LinkingSummaryIncluded,
    IReadOnlyDictionary<string, long> SourceExcludedRowCounts);

internal sealed record AbwabSnapshotRestorePolicy(
    string Target,
    bool PreserveExplicitIds,
    bool ResetIdentitySequences,
    bool RequireEmptyTargets,
    bool RequireEmptyInclusionSyncs);

internal sealed record AbwabSnapshotSchemaColumn(
    string Table,
    string Column,
    string DataType,
    bool Nullable,
    int Position);

internal sealed record AbwabSnapshotTableCount(int Total, int? Active = null, int? Archived = null);

internal sealed record AbwabSnapshotReadResult(
    AbwabSnapshotDocument Snapshot,
    IReadOnlyList<string> ActualAbwabTables,
    IReadOnlyDictionary<string, long> DatabaseCounts,
    string TransactionIsolation);

internal sealed record AbwabSnapshotValidationResult(
    bool Succeeded,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

internal sealed record AbwabSnapshotArtifactPaths(
    string Snapshot,
    string Checksum,
    string JsonReport,
    string MarkdownReport);

internal sealed record AbwabSnapshotAuditReport(
    string Operation,
    string Verdict,
    bool Persisted,
    DateTimeOffset RunAtUtc,
    string MaskedTarget,
    string? SnapshotPath,
    string? SnapshotSha256,
    string Format,
    int FormatVersion,
    IReadOnlyDictionary<string, AbwabSnapshotTableCount> Counts,
    IReadOnlyDictionary<string, long> SourceExcludedRowCounts,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
