using System.Linq.Expressions;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Timeline;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

public sealed class EfAbwabCoreReadPort(QuranDashboardDbContext db, IServerClock clock) : IAbwabCoreReadPort
{
    public async Task<AbwabTreeSnapshotDto> GetTreeSnapshotAsync(CancellationToken cancellationToken)
    {
        var revision = await GetRevisionStateAsync(cancellationToken);

        var sections = await db.AbwabSections
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectionSnapshotDto(s.SectionId, s.Name, s.NormalizedName, s.SortOrder, s.IsPermanentDefault))
            .ToListAsync(cancellationToken);

        var categories = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .Select(ToSnapshot)
            .ToListAsync(cancellationToken);

        var allCategoriesProjection = categories
            .Where(c => c.ParentCategoryId is null)
            .OrderBy(c => c.GlobalOrder)
            .ToList();

        return new AbwabTreeSnapshotDto(
            ExpectedTimelineGeneration.Of(revision.TimelineGeneration),
            revision.TreeRevision,
            AbwabTreeSnapshotDto.CurrentSchemaVersion,
            clock.UtcNow,
            sections,
            categories,
            allCategoriesProjection);
    }

    public async Task<CategorySearchResultDto> SearchCategoriesAsync(string query, CancellationToken cancellationToken)
    {
        var revision = await GetRevisionStateAsync(cancellationToken);
        var normalizedQuery = ArabicNameNormalizer.Normalize(query);

        if (string.IsNullOrEmpty(normalizedQuery))
        {
            return new CategorySearchResultDto(ExpectedTimelineGeneration.Of(revision.TimelineGeneration), []);
        }

        var matchedByName = db.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.NormalizedName.Contains(normalizedQuery))
            .Select(c => c.CategoryId);

        var matchedByAlias = db.AbwabCategorySearchAliases
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.NormalizedValue.Contains(normalizedQuery))
            .Select(a => a.CategoryId);

        var matchedIds = await matchedByName.Union(matchedByAlias).ToListAsync(cancellationToken);

        var matches = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && matchedIds.Contains(c.CategoryId))
            .Select(ToSnapshot)
            .ToListAsync(cancellationToken);

        return new CategorySearchResultDto(ExpectedTimelineGeneration.Of(revision.TimelineGeneration), matches);
    }

    private static readonly Expression<Func<Category, CategorySnapshotDto>> ToSnapshot =
        c => new CategorySnapshotDto(
            c.CategoryId,
            c.Name,
            c.NormalizedName,
            c.RepresentativeQuranExcerpt,
            c.Description,
            c.ParentCategoryId,
            c.SectionId,
            c.SiblingOrder,
            c.SectionOrder,
            c.GlobalOrder,
            c.AncestorIds,
            c.Depth,
            c.CategoryContentRevision);

    private async Task<AbwabRevisionState> GetRevisionStateAsync(CancellationToken cancellationToken) =>
        await db.AbwabRevisionStates.AsNoTracking().SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken);
}
