namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingAyahPreflightDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    string Classification,
    IReadOnlyList<LinkingOverlappingSourceDto> OverlappingSources,
    LinkingWordChangesDto WordChanges,
    LinkingDoorWordImpactDto DoorWordImpact,
    LinkingDescriptionChangesDto DescriptionChanges,
    string? InvalidReason);

public sealed record LinkingOverlappingSourceDto(
    string SourceIdentity,
    string Label,
    string SourceKind);

public sealed record LinkingWordChangesDto(
    IReadOnlyList<int> Added,
    IReadOnlyList<int> Removed,
    IReadOnlyList<int> Unchanged);

public sealed record LinkingDoorWordImpactDto(
    IReadOnlyList<int> Added,
    IReadOnlyList<int> Existing,
    IReadOnlyList<int> Removed);

public sealed record LinkingDescriptionChangesDto(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Unchanged);
