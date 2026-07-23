using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public interface ICategorySearchAliasWriteStore
{
    Task<CategorySearchAlias?> FindTrackedAsync(Guid categorySearchAliasId, CancellationToken cancellationToken);

    Task<bool> ActiveNormalizedValueExistsAsync(Guid categoryId, string normalizedValue, Guid? excludeAliasId, CancellationToken cancellationToken);

    void Add(CategorySearchAlias alias);
}
