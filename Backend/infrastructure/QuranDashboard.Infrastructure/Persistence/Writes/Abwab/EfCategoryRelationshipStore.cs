using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfCategoryRelationshipStore(QuranDashboardDbContext db) : ICategoryRelationshipStore
{
    public Task<CategoryRelationship?> FindTrackedAsync(Guid categoryRelationshipId, CancellationToken cancellationToken) =>
        db.AbwabCategoryRelationships.SingleOrDefaultAsync(r => r.CategoryRelationshipId == categoryRelationshipId, cancellationToken);

    public Task<bool> ActiveMutualPairExistsAsync(
        Guid lowerCategoryId,
        Guid higherCategoryId,
        RelationshipType relationshipType,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken) =>
        db.AbwabCategoryRelationships.AnyAsync(
            r => !r.IsDeleted &&
                r.LowerCategoryId == lowerCategoryId &&
                r.HigherCategoryId == higherCategoryId &&
                r.RelationshipType == relationshipType &&
                r.CategoryRelationshipId != excludeCategoryRelationshipId,
            cancellationToken);

    public Task<bool> ActiveDirectionalEdgeExistsAsync(
        Guid sourceCategoryId,
        Guid targetCategoryId,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken) =>
        db.AbwabCategoryRelationships.AnyAsync(
            r => !r.IsDeleted &&
                r.RelationshipType == RelationshipType.BroaderNarrower &&
                r.SourceCategoryId == sourceCategoryId &&
                r.TargetCategoryId == targetCategoryId &&
                r.CategoryRelationshipId != excludeCategoryRelationshipId,
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetActiveDirectionalTargetsAsync(
        IReadOnlyCollection<Guid> sourceCategoryIds,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken) =>
        await db.AbwabCategoryRelationships
            .Where(r => !r.IsDeleted &&
                r.RelationshipType == RelationshipType.BroaderNarrower &&
                r.SourceCategoryId != null &&
                sourceCategoryIds.Contains(r.SourceCategoryId.Value) &&
                r.CategoryRelationshipId != excludeCategoryRelationshipId)
            .Select(r => r.TargetCategoryId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

    public void Add(CategoryRelationship relationship) => db.AbwabCategoryRelationships.Add(relationship);
}
