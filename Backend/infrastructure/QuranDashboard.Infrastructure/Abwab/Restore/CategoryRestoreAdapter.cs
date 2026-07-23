using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

// One adapter for the whole Category aggregate (§8): CategorySearchAlias and all three order
// fields (SiblingOrder/SectionOrder/GlobalOrder) round-trip here as facets, not as separate adapters.
public sealed class CategoryRestoreAdapter : IAbwabRestoreAdapter<CategoryAggregate, CategoryRestoreSnapshot>
{
    public const int Schema = 1;

    public string PersistedType => "Category";

    public int SnapshotSchemaVersion => Schema;

    public CategoryRestoreSnapshot Capture(CategoryAggregate product)
    {
        var category = product.Category;

        return new CategoryRestoreSnapshot(
            Schema,
            category.CategoryId,
            category.Name,
            category.NormalizedName,
            category.RepresentativeQuranExcerpt,
            category.Description,
            category.ParentCategoryId,
            category.SectionId,
            category.SiblingOrder,
            category.SectionOrder,
            category.GlobalOrder,
            category.AncestorIds,
            category.Depth,
            category.OrdinaryProtectionActorSubject,
            category.OrdinaryProtectionLastEditedAtUtc,
            category.IsDeleted,
            category.DeletedAtUtc,
            [.. product.Aliases.Select(CaptureAlias)]);
    }

    public CategoryAggregate Reconstruct(CategoryRestoreSnapshot snapshot)
    {
        var category = new Category
        {
            CategoryId = snapshot.CategoryId,
            Name = snapshot.Name,
            NormalizedName = snapshot.NormalizedName,
            RepresentativeQuranExcerpt = snapshot.RepresentativeQuranExcerpt,
            Description = snapshot.Description,
            ParentCategoryId = snapshot.ParentCategoryId,
            SectionId = snapshot.SectionId,
            SiblingOrder = snapshot.SiblingOrder,
            SectionOrder = snapshot.SectionOrder,
            GlobalOrder = snapshot.GlobalOrder,
            AncestorIds = [.. snapshot.AncestorIds],
            Depth = snapshot.Depth,
            OrdinaryProtectionActorSubject = snapshot.OrdinaryProtectionActorSubject,
            OrdinaryProtectionLastEditedAtUtc = snapshot.OrdinaryProtectionLastEditedAtUtc,
            IsDeleted = snapshot.IsDeleted,
            DeletedAtUtc = snapshot.DeletedAtUtc,
        };

        var aliases = snapshot.Aliases.Select(a => ReconstructAlias(a, category.CategoryId)).ToList();
        return new CategoryAggregate(category, aliases);
    }

    private static CategorySearchAliasRestoreSnapshot CaptureAlias(CategorySearchAlias alias) => new(
        alias.CategorySearchAliasId,
        alias.Value,
        alias.NormalizedValue,
        alias.IsDeleted,
        alias.DeletedAtUtc);

    private static CategorySearchAlias ReconstructAlias(CategorySearchAliasRestoreSnapshot snapshot, Guid categoryId) => new()
    {
        CategorySearchAliasId = snapshot.CategorySearchAliasId,
        CategoryId = categoryId,
        Value = snapshot.Value,
        NormalizedValue = snapshot.NormalizedValue,
        IsDeleted = snapshot.IsDeleted,
        DeletedAtUtc = snapshot.DeletedAtUtc,
    };
}
