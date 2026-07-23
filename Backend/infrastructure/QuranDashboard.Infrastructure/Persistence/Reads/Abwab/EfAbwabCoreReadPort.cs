using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Timeline;
using QuranDashboard.Domain.Abwab.Tree;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

// Always reads the FULL product (protection detail included); redaction by caller permission is a
// separate backend projection step (Application.Abwab.Tree.AbwabCompositeReadRedactor, T060) — this
// port never redacts.
public sealed class EfAbwabCoreReadPort(QuranDashboardDbContext db, IServerClock clock) : IAbwabCoreReadPort
{
    public async Task<AbwabTreeSnapshotDto> GetTreeSnapshotAsync(CancellationToken cancellationToken)
    {
        var revision = await GetRevisionStateAsync(cancellationToken);
        var serverTimeUtc = clock.UtcNow;

        var sections = await db.AbwabSections
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectionSnapshotDto(s.SectionId, s.Name, s.NormalizedName, s.SortOrder, s.IsPermanentDefault, s.Version))
            .ToListAsync(cancellationToken);

        var categoryEntities = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .ToListAsync(cancellationToken);

        var protections = await db.AbwabManualProtections
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);
        var protectionsByCategory = protections.ToLookup(p => p.CategoryId);

        var aliases = await db.AbwabCategorySearchAliases
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .ToListAsync(cancellationToken);
        var aliasesByCategory = aliases.ToLookup(a => a.CategoryId);

        var categories = categoryEntities
            .Select(c => ToSnapshot(c, protectionsByCategory, aliasesByCategory, serverTimeUtc))
            .ToList();

        var allCategoriesProjection = categories
            .Where(c => c.ParentCategoryId is null)
            .OrderBy(c => c.GlobalOrder)
            .ToList();

        return new AbwabTreeSnapshotDto(
            ExpectedTimelineGeneration.Of(revision.TimelineGeneration),
            revision.TreeRevision,
            AbwabTreeSnapshotDto.CurrentSchemaVersion,
            serverTimeUtc,
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

        var matchEntities = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && matchedIds.Contains(c.CategoryId))
            .ToListAsync(cancellationToken);

        var serverTimeUtc = clock.UtcNow;
        var protections = await db.AbwabManualProtections
            .AsNoTracking()
            .Where(p => !p.IsDeleted && matchedIds.Contains(p.CategoryId))
            .ToListAsync(cancellationToken);
        var protectionsByCategory = protections.ToLookup(p => p.CategoryId);

        var aliases = await db.AbwabCategorySearchAliases
            .AsNoTracking()
            .Where(a => !a.IsDeleted && matchedIds.Contains(a.CategoryId))
            .ToListAsync(cancellationToken);
        var aliasesByCategory = aliases.ToLookup(a => a.CategoryId);

        var matches = matchEntities.Select(c => ToSnapshot(c, protectionsByCategory, aliasesByCategory, serverTimeUtc)).ToList();

        return new CategorySearchResultDto(ExpectedTimelineGeneration.Of(revision.TimelineGeneration), matches);
    }

    private static CategorySnapshotDto ToSnapshot(
        Category c,
        ILookup<Guid, ManualProtection> protectionsByCategory,
        ILookup<Guid, CategorySearchAlias> aliasesByCategory,
        DateTimeOffset serverTimeUtc) =>
        new(
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
            c.CategoryContentRevision,
            c.Version,
            aliasesByCategory[c.CategoryId].Select(a => new CategorySearchAliasDto(a.CategorySearchAliasId, a.Value, a.Version)).ToList(),
            AbwabProtectionSummaryProjector.Build(c, protectionsByCategory, serverTimeUtc));

    private async Task<AbwabRevisionState> GetRevisionStateAsync(CancellationToken cancellationToken) =>
        await db.AbwabRevisionStates.AsNoTracking().SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken);
}
