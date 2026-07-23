using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfCategoryTreeStore(QuranDashboardDbContext db) : ICategoryTreeStore
{
    public Task<Category?> FindTrackedAsync(Guid categoryId, bool includeDeleted, CancellationToken cancellationToken) =>
        includeDeleted
            ? db.AbwabCategories.SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken)
            : db.AbwabCategories.SingleOrDefaultAsync(c => c.CategoryId == categoryId && !c.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Category>> FindManyTrackedAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken) =>
        await db.AbwabCategories.Where(c => categoryIds.Contains(c.CategoryId) && !c.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindDescendantsAsync(Guid ancestorCategoryId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => !c.IsDeleted && c.AncestorIds.Contains(ancestorCategoryId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindActiveSiblingsAsync(Guid parentCategoryId, CancellationToken cancellationToken) =>
        await db.AbwabCategories.Where(c => c.ParentCategoryId == parentCategoryId && !c.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindActiveSectionRootsAsync(Guid sectionId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => c.ParentCategoryId == null && c.SectionId == sectionId && !c.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindActiveGlobalRootsAsync(CancellationToken cancellationToken) =>
        await db.AbwabCategories.Where(c => c.ParentCategoryId == null && !c.IsDeleted).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Category>> FindByDeletionOperationAsync(Guid deletionOperationId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => c.IsDeleted && c.DeletionOperationId == deletionOperationId)
            .OrderBy(c => c.Depth)
            .ThenBy(c => c.CategoryId)
            .ToListAsync(cancellationToken);

    public Task<bool> ActiveNormalizedNameExistsAsync(string normalizedName, Guid? parentCategoryId, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(
            c => !c.IsDeleted && c.ParentCategoryId == parentCategoryId && c.NormalizedName == normalizedName && c.CategoryId != excludeCategoryId,
            cancellationToken);

    public async Task<int?> GetMaxSiblingOrderAsync(Guid parentCategoryId, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => c.ParentCategoryId == parentCategoryId && !c.IsDeleted && c.CategoryId != excludeCategoryId)
            .Select(c => (int?)c.SiblingOrder)
            .MaxAsync(cancellationToken);

    public async Task<int?> GetMaxSectionOrderAsync(Guid sectionId, Guid? excludeCategoryId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => c.ParentCategoryId == null && c.SectionId == sectionId && !c.IsDeleted && c.CategoryId != excludeCategoryId)
            .Select(c => (int?)c.SectionOrder)
            .MaxAsync(cancellationToken);

    public async Task<int?> GetMaxGlobalOrderAsync(Guid? excludeCategoryId, CancellationToken cancellationToken) =>
        await db.AbwabCategories
            .Where(c => c.ParentCategoryId == null && !c.IsDeleted && c.CategoryId != excludeCategoryId)
            .Select(c => (int?)c.GlobalOrder)
            .MaxAsync(cancellationToken);

    public Task<bool> HasActiveSiblingWithOrderAsync(Guid parentCategoryId, int order, Guid excludeCategoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(
            c => c.ParentCategoryId == parentCategoryId && !c.IsDeleted && c.CategoryId != excludeCategoryId && c.SiblingOrder == order,
            cancellationToken);

    public Task<bool> HasActiveSectionRootWithOrderAsync(Guid sectionId, int order, Guid excludeCategoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(
            c => c.ParentCategoryId == null && c.SectionId == sectionId && !c.IsDeleted && c.CategoryId != excludeCategoryId && c.SectionOrder == order,
            cancellationToken);

    public Task<bool> HasActiveGlobalRootWithOrderAsync(int order, Guid excludeCategoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(
            c => c.ParentCategoryId == null && !c.IsDeleted && c.CategoryId != excludeCategoryId && c.GlobalOrder == order,
            cancellationToken);

    public Task<bool> ExistsActiveAsync(Guid categoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(c => c.CategoryId == categoryId && !c.IsDeleted, cancellationToken);

    public Task<bool> ExistsAsync(Guid categoryId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(c => c.CategoryId == categoryId, cancellationToken);

    public void Add(Category category) => db.AbwabCategories.Add(category);
}
