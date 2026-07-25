using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abstractions.Abwab.Relationships;

public interface ICategoryRelationshipStore
{
    Task<CategoryRelationship?> FindTrackedAsync(Guid categoryRelationshipId, CancellationToken cancellationToken);

    Task<bool> ActiveMutualPairExistsAsync(
        Guid lowerCategoryId,
        Guid higherCategoryId,
        RelationshipType relationshipType,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken);

    Task<bool> ActiveDirectionalEdgeExistsAsync(
        Guid sourceCategoryId,
        Guid targetCategoryId,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken);

    // One breadth-first layer of the active broader→narrower graph, so cycle validation walks only
    // the reachable subgraph instead of loading every edge in the database.
    Task<IReadOnlyList<Guid>> GetActiveDirectionalTargetsAsync(
        IReadOnlyCollection<Guid> sourceCategoryIds,
        Guid? excludeCategoryRelationshipId,
        CancellationToken cancellationToken);

    void Add(CategoryRelationship relationship);
}
