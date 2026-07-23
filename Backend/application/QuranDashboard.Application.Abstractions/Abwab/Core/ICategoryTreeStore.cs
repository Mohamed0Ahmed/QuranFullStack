using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public interface ICategoryTreeStore
{
    Task<Category?> FindTrackedAsync(Guid categoryId, bool includeDeleted, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindManyTrackedAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindDescendantsAsync(Guid ancestorCategoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindActiveSiblingsAsync(Guid parentCategoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindActiveSectionRootsAsync(Guid sectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindActiveGlobalRootsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> FindByDeletionOperationAsync(Guid deletionOperationId, CancellationToken cancellationToken);

    Task<bool> ActiveNormalizedNameExistsAsync(string normalizedName, Guid? parentCategoryId, Guid? excludeCategoryId, CancellationToken cancellationToken);

    Task<int?> GetMaxSiblingOrderAsync(Guid parentCategoryId, Guid? excludeCategoryId, CancellationToken cancellationToken);

    Task<int?> GetMaxSectionOrderAsync(Guid sectionId, Guid? excludeCategoryId, CancellationToken cancellationToken);

    Task<int?> GetMaxGlobalOrderAsync(Guid? excludeCategoryId, CancellationToken cancellationToken);

    Task<bool> HasActiveSiblingWithOrderAsync(Guid parentCategoryId, int order, Guid excludeCategoryId, CancellationToken cancellationToken);

    Task<bool> HasActiveSectionRootWithOrderAsync(Guid sectionId, int order, Guid excludeCategoryId, CancellationToken cancellationToken);

    Task<bool> HasActiveGlobalRootWithOrderAsync(int order, Guid excludeCategoryId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid categoryId, CancellationToken cancellationToken);

    void Add(Category category);
}
