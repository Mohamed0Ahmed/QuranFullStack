using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

// Dormancy is a read-side projection over untouched rows, so a relationship snapshot carries no
// dormancy state: restoring the category endpoint alone re-exposes the row.
public sealed class RelationshipRestoreAdapter : IAbwabRestoreAdapter<CategoryRelationship, RelationshipRestoreSnapshot>
{
    public const int Schema = 1;

    public string PersistedType => "Relationship";

    public int SnapshotSchemaVersion => Schema;

    public RelationshipRestoreSnapshot Capture(CategoryRelationship product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return new RelationshipRestoreSnapshot(
            Schema,
            product.CategoryRelationshipId,
            product.RelationshipType,
            product.LowerCategoryId,
            product.HigherCategoryId,
            product.SourceCategoryId,
            product.TargetCategoryId,
            product.IsDeleted,
            product.DeletedAtUtc);
    }

    public CategoryRelationship Reconstruct(RelationshipRestoreSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new CategoryRelationship
        {
            CategoryRelationshipId = snapshot.CategoryRelationshipId,
            RelationshipType = snapshot.RelationshipType,
            LowerCategoryId = snapshot.LowerCategoryId,
            HigherCategoryId = snapshot.HigherCategoryId,
            SourceCategoryId = snapshot.SourceCategoryId,
            TargetCategoryId = snapshot.TargetCategoryId,
            IsDeleted = snapshot.IsDeleted,
            DeletedAtUtc = snapshot.DeletedAtUtc,
        };
    }
}
