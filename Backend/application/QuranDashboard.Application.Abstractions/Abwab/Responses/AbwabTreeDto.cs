namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

public sealed record AbwabTreeDto(
    DateTimeOffset? Version,
    IReadOnlyList<AbwabTreeSectionDto> Sections,
    IReadOnlyList<AbwabTreeDoorDto> Doors);

public sealed record AbwabTreeSectionDto(
    int Id,
    string Name,
    int OrderValue,
    uint Version,
    int DoorsInScopeCount);

public sealed record AbwabTreeDoorDto(
    int Id,
    int SectionId,
    bool SectionRetired,
    int? ParentId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    int OrderValue,
    int? GlobalOrderValue,
    uint Version,
    bool IsArchived,
    int DirectChildCount,
    int RelationCount,
    int LinkCount,
    int SelectedWordCount,
    IReadOnlyList<string> Aliases);
