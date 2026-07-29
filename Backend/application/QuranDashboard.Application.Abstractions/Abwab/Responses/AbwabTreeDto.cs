namespace QuranDashboard.Application.Abstractions.Abwab.Responses;

// Flat, not nested: doors carry their own SectionId/ParentId, so the client assembles the tree at any
// depth (§4 "Depth: No limit") instead of the read model recursing to an arbitrary level. DirectChildCount
// would be a redundant, always-derivable field if children were nested arrays — its presence here is the
// read model's own proof that the shape is deliberately flat.
public sealed record AbwabTreeDto(
    DateTimeOffset? Version,
    IReadOnlyList<AbwabTreeSectionDto> Sections,
    IReadOnlyList<AbwabTreeDoorDto> Doors);

// DoorsInScopeCount counts only LIVE doors in the section (any depth, via SectionId inheritance) — the
// "how many doors does this section currently have" badge, not inflated by archived subtrees.
public sealed record AbwabTreeSectionDto(
    int Id,
    string Name,
    int OrderValue,
    uint Version,
    int DoorsInScopeCount);

// Archived doors are included (never omitted) and flagged via IsArchived, per §4's "archived included,
// flagged" read-model decision — restore needs them visible. DirectChildCount mirrors the same
// live-only convention as DoorsInScopeCount, for the same reason.
public sealed record AbwabTreeDoorDto(
    int Id,
    int? SectionId,
    int? ParentId,
    string Name,
    string? Description,
    string? RepresentativeAyahText,
    int OrderValue,
    int? GlobalOrderValue,
    uint Version,
    bool IsArchived,
    int DirectChildCount,
    IReadOnlyList<string> Aliases);
