using QuranDashboard.Domain.Abwab.Sections;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public interface ISectionWriteStore
{
    Task<Section?> FindTrackedAsync(Guid sectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Section>> FindManyTrackedAsync(IReadOnlyCollection<Guid> sectionIds, CancellationToken cancellationToken);

    Task<bool> ActiveNormalizedNameExistsAsync(string normalizedName, Guid? excludeSectionId, CancellationToken cancellationToken);

    Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken);

    Task<bool> HasActiveRootCategoriesAsync(Guid sectionId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveAsync(Guid sectionId, CancellationToken cancellationToken);

    Task<Guid> GetPermanentDefaultSectionIdAsync(CancellationToken cancellationToken);

    void Add(Section section);
}
