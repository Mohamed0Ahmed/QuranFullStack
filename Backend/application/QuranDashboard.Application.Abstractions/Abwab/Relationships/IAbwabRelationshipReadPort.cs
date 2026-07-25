namespace QuranDashboard.Application.Abstractions.Abwab.Relationships;

// Dormancy is derived here, never stored: a relationship whose endpoint category is soft-deleted is
// filtered out of the actionable projection and reappears untouched once that endpoint is restored.
public interface IAbwabRelationshipReadPort
{
    Task<CategoryRelationshipListDto> GetForCategoryAsync(Guid categoryId, bool includeDeleted, CancellationToken cancellationToken);

    Task<DormantRelationshipCountsDto> GetDormantCountsAsync(
        IReadOnlyCollection<Guid> affectedCategoryIds,
        CancellationToken cancellationToken);
}
