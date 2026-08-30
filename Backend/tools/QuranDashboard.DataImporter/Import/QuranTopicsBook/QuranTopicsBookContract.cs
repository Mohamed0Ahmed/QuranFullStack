using System.Text.Json.Serialization;

namespace QuranDashboard.DataImporter.Import.QuranTopicsBook;

internal static class QuranTopicsBookContract
{
    internal const string Format = "quran-dashboard-quran-topics-book";
    internal const int FormatVersion = 1;
    internal const string DirectOnlyParentAyahPolicy = "direct_only";
    internal const string ConsecutiveRangesGroupingPolicy = "consecutive_ranges_grouped";
    internal const string SingleGroupKind = "single";
    internal const string ConsecutiveRangeGroupKind = "consecutive_range";

    internal static IReadOnlyList<string> EmptyTargetTables { get; } =
    [
        "abwab_sections",
        "abwab_doors",
        "abwab_door_aliases",
        "abwab_door_relations",
        "abwab_door_inclusions",
        "abwab_door_inclusion_unit_syncs",
        "abwab_templates",
        "abwab_template_nodes",
        "linking_confirmation_jobs",
        "linking_operations",
        "linking_prepared_affected_contributions",
        "linking_prepared_ayah_descriptions",
        "linking_prepared_ayah_words",
        "linking_prepared_ayahs",
        "linking_prepared_units",
        "linking_prepared_sources",
        "linking_prepared_preflights",
        "linking_source_contribution_units",
        "linking_source_contributions",
        "linking_unit_ayah_descriptions",
        "linking_unit_ayah_words",
        "linking_unit_ayahs",
        "linking_units",
        "linking_door_ayah_words",
        "linking_door_ayahs",
    ];
}

internal sealed record QuranTopicsBookDocument(
    string Format,
    int FormatVersion,
    string Title,
    QuranTopicsBookSource Source,
    QuranTopicsBookPolicy Policy,
    IReadOnlyList<QuranTopicsBookSection> Sections);

internal sealed record QuranTopicsBookSource(
    string FileName,
    string Sha256,
    int PdfPageFrom,
    int PdfPageTo);

internal sealed record QuranTopicsBookPolicy(
    string ParentAyahPolicy,
    string GroupingPolicy);

internal sealed record QuranTopicsBookSection(
    string Key,
    string Name,
    int Order,
    IReadOnlyList<QuranTopicsBookDoor> Doors);

internal sealed record QuranTopicsBookDoor(
    string Key,
    string? ParentKey,
    string Name,
    int Order,
    int? GlobalOrder,
    IReadOnlyList<int> PdfPages,
    IReadOnlyList<QuranTopicsBookAyahGroup> AyahGroups);

internal sealed record QuranTopicsBookAyahGroup(
    int Order,
    string Kind,
    IReadOnlyList<string> VerseKeys);

internal sealed record QuranTopicsBookSourcePackage(
    string SourcePath,
    string Sha256,
    QuranTopicsBookDocument Document,
    QuranTopicsBookMetrics Metrics,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings);

internal sealed record QuranTopicsBookMetrics(
    int SectionCount,
    int DoorCount,
    int ParentDoorCount,
    int LeafDoorCount,
    int AyahGroupCount,
    int GroupedRangeCount,
    int AyahReferenceCount,
    int UniqueVerseKeyCount);

internal sealed record QuranTopicsBookImportResult(
    string Verdict,
    string Persisted,
    QuranTopicsBookMetrics Metrics,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

internal sealed record QuranTopicsBookAuditReport(
    int SchemaVersion,
    DateTimeOffset RunAtUtc,
    string SourcePath,
    string SourceSha256,
    string? Target,
    int ActorUserId,
    bool ValidateOnly,
    string Verdict,
    string Persisted,
    QuranTopicsBookMetrics? Metrics,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

internal sealed class QuranTopicsBookImportException(
    string message,
    IReadOnlyList<string>? checks = null,
    IReadOnlyList<string>? warnings = null) : Exception(message)
{
    internal IReadOnlyList<string> Checks { get; } = checks ?? [];
    internal IReadOnlyList<string> Warnings { get; } = warnings ?? [];
}

internal sealed class QuranTopicsBookCommitUnknownException : Exception
{
    internal QuranTopicsBookCommitUnknownException() : base(
        "The commit acknowledgement was ambiguous; inspect the target before retrying.")
    {
    }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = true)]
[JsonSerializable(typeof(QuranTopicsBookDocument))]
[JsonSerializable(typeof(QuranTopicsBookAuditReport))]
internal partial class QuranTopicsBookJsonContext : JsonSerializerContext;
