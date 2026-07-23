namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record CategorySnapshotDto(
    Guid CategoryId,
    string Name,
    string NormalizedName,
    string? RepresentativeQuranExcerpt,
    string? Description,
    Guid? ParentCategoryId,
    Guid? SectionId,
    int? SiblingOrder,
    int? SectionOrder,
    int? GlobalOrder,
    IReadOnlyList<Guid> AncestorIds,
    int Depth,
    long CategoryContentRevision,
    uint Version,
    IReadOnlyList<CategorySearchAliasDto> Aliases,
    CategoryProtectionSummaryDto Protection);
