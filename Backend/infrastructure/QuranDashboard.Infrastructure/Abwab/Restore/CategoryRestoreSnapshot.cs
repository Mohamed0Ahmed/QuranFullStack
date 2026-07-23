namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record CategoryRestoreSnapshot(
    int SchemaVersion,
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
    string? OrdinaryProtectionActorSubject,
    DateTimeOffset? OrdinaryProtectionLastEditedAtUtc,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    IReadOnlyList<CategorySearchAliasRestoreSnapshot> Aliases);
