using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Sections;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfSectionWriteStore(QuranDashboardDbContext db) : ISectionWriteStore
{
    public Task<Section?> FindTrackedAsync(Guid sectionId, CancellationToken cancellationToken) =>
        db.AbwabSections.SingleOrDefaultAsync(s => s.SectionId == sectionId && !s.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Section>> FindManyTrackedAsync(IReadOnlyCollection<Guid> sectionIds, CancellationToken cancellationToken) =>
        await db.AbwabSections
            .Where(s => sectionIds.Contains(s.SectionId) && !s.IsDeleted)
            .OrderBy(s => s.SectionId)
            .ToListAsync(cancellationToken);

    public Task<bool> ActiveNormalizedNameExistsAsync(string normalizedName, Guid? excludeSectionId, CancellationToken cancellationToken) =>
        db.AbwabSections.AnyAsync(
            s => !s.IsDeleted && s.NormalizedName == normalizedName && s.SectionId != excludeSectionId,
            cancellationToken);

    public async Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken) =>
        await db.AbwabSections.Where(s => !s.IsDeleted).Select(s => (int?)s.SortOrder).MaxAsync(cancellationToken) ?? -1;

    public Task<bool> HasActiveRootCategoriesAsync(Guid sectionId, CancellationToken cancellationToken) =>
        db.AbwabCategories.AnyAsync(c => c.SectionId == sectionId && !c.IsDeleted, cancellationToken);

    public Task<bool> ExistsActiveAsync(Guid sectionId, CancellationToken cancellationToken) =>
        db.AbwabSections.AnyAsync(s => s.SectionId == sectionId && !s.IsDeleted, cancellationToken);

    public async Task<Guid> GetPermanentDefaultSectionIdAsync(CancellationToken cancellationToken) =>
        (await db.AbwabSections.SingleAsync(s => s.IsPermanentDefault && !s.IsDeleted, cancellationToken)).SectionId;

    public void Add(Section section) => db.AbwabSections.Add(section);
}
