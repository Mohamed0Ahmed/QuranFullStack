using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfCategorySearchAliasWriteStore(QuranDashboardDbContext db) : ICategorySearchAliasWriteStore
{
    public Task<CategorySearchAlias?> FindTrackedAsync(Guid categorySearchAliasId, CancellationToken cancellationToken) =>
        db.AbwabCategorySearchAliases.SingleOrDefaultAsync(a => a.CategorySearchAliasId == categorySearchAliasId && !a.IsDeleted, cancellationToken);

    public Task<bool> ActiveNormalizedValueExistsAsync(Guid categoryId, string normalizedValue, Guid? excludeAliasId, CancellationToken cancellationToken) =>
        db.AbwabCategorySearchAliases.AnyAsync(
            a => !a.IsDeleted && a.CategoryId == categoryId && a.NormalizedValue == normalizedValue && a.CategorySearchAliasId != excludeAliasId,
            cancellationToken);

    public void Add(CategorySearchAlias alias) => db.AbwabCategorySearchAliases.Add(alias);
}
