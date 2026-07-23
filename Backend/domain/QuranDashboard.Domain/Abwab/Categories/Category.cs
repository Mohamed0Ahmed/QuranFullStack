using QuranDashboard.Domain.Abwab.Persistence;

namespace QuranDashboard.Domain.Abwab.Categories;

public sealed class Category : IAbwabAuditable
{
    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? RepresentativeQuranExcerpt { get; set; }

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public Guid? SectionId { get; set; }

    public int? SiblingOrder { get; set; }

    public int? SectionOrder { get; set; }

    public int? GlobalOrder { get; set; }

    public List<Guid> AncestorIds { get; set; } = [];

    public int Depth { get; set; }

    public string? OrdinaryProtectionActorSubject { get; set; }

    public DateTimeOffset? OrdinaryProtectionLastEditedAtUtc { get; set; }

    public long CategoryContentRevision { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    // Correlates every category soft-deleted by the same atomic subtree-delete operation, so
    // operation-restore can find exactly that set; cleared back to null on restore.
    public Guid? DeletionOperationId { get; set; }

    public uint Version { get; set; }

    public bool IsRoot => ParentCategoryId is null;

    public bool HasValidShape() => IsRoot ? HasValidRootShape() : HasValidDescendantShape();

    private bool HasValidRootShape() =>
        SectionId is not null &&
        SiblingOrder is null &&
        SectionOrder is not null &&
        GlobalOrder is not null &&
        AncestorIds.Count == 0 &&
        Depth == 0;

    private bool HasValidDescendantShape() =>
        SectionId is null &&
        SiblingOrder is not null &&
        SectionOrder is null &&
        GlobalOrder is null &&
        Depth == AncestorIds.Count;
}
