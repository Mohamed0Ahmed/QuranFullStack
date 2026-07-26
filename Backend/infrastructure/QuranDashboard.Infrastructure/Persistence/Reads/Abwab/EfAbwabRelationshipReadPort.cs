using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

public sealed class EfAbwabRelationshipReadPort(QuranDashboardDbContext db, IServerClock clock) : IAbwabRelationshipReadPort
{
    public const int SchemaVersion = 1;

    public async Task<CategoryRelationshipListDto> GetForCategoryAsync(
        Guid categoryId,
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        var rows = await db.AbwabCategoryRelationships
            .AsNoTracking()
            .Where(r => r.LowerCategoryId == categoryId || r.HigherCategoryId == categoryId ||
                r.SourceCategoryId == categoryId || r.TargetCategoryId == categoryId)
            .Where(r => includeDeleted || !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var endpointIds = rows.SelectMany(row => row.EndpointCategoryIds).Distinct().ToList();
        var endpoints = await LoadEndpointsAsync(endpointIds, cancellationToken);

        // Dormancy is derived, never stored: a row whose endpoint category is soft-deleted is simply
        // absent from the actionable projection, and reappears untouched when that endpoint returns.
        var visible = rows
            .Where(row => row.EndpointCategoryIds.All(id => !endpoints[id].IsDeleted))
            .Select(row => ToDto(row, endpoints))
            .ToList();

        var generation = await CurrentGenerationAsync(cancellationToken);

        return new CategoryRelationshipListDto(
            SchemaVersion, categoryId, ExpectedTimelineGeneration.Of(generation), clock.UtcNow, visible);
    }

    public async Task<DormantRelationshipCountsDto> GetDormantCountsAsync(
        IReadOnlyCollection<Guid> affectedCategoryIds,
        CancellationToken cancellationToken)
    {
        if (affectedCategoryIds.Count == 0)
        {
            return new DormantRelationshipCountsDto(0, []);
        }

        var attached = await db.AbwabCategoryRelationships
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Where(r =>
                (r.LowerCategoryId != null && affectedCategoryIds.Contains(r.LowerCategoryId.Value)) ||
                (r.HigherCategoryId != null && affectedCategoryIds.Contains(r.HigherCategoryId.Value)) ||
                (r.SourceCategoryId != null && affectedCategoryIds.Contains(r.SourceCategoryId.Value)) ||
                (r.TargetCategoryId != null && affectedCategoryIds.Contains(r.TargetCategoryId.Value)))
            .ToListAsync(cancellationToken);

        var endpoints = await LoadEndpointsAsync(attached.SelectMany(row => row.EndpointCategoryIds).Distinct().ToList(), cancellationToken);

        var dormant = attached
            .Where(row => row.EndpointCategoryIds.Any(id => endpoints[id].IsDeleted))
            .ToList();

        var byType = dormant
            .GroupBy(row => row.RelationshipType)
            .OrderBy(group => group.Key)
            .Select(group => new DormantRelationshipCountDto(group.Key, group.Count()))
            .ToList();

        return new DormantRelationshipCountsDto(dormant.Count, byType);
    }

    private async Task<IReadOnlyDictionary<Guid, EndpointRow>> LoadEndpointsAsync(
        IReadOnlyCollection<Guid> endpointIds,
        CancellationToken cancellationToken)
    {
        if (endpointIds.Count == 0)
        {
            return new Dictionary<Guid, EndpointRow>();
        }

        var endpoints = await db.AbwabCategories
            .AsNoTracking()
            .Where(c => endpointIds.Contains(c.CategoryId))
            .Select(c => new { c.CategoryId, c.Name, c.SectionId, c.AncestorIds, c.IsDeleted })
            .ToListAsync(cancellationToken);

        var ancestorIds = endpoints.SelectMany(e => e.AncestorIds).Distinct().ToList();
        var ancestorNames = ancestorIds.Count == 0
            ? []
            : await db.AbwabCategories
                .AsNoTracking()
                .Where(c => ancestorIds.Contains(c.CategoryId))
                .ToDictionaryAsync(c => c.CategoryId, c => c.Name, cancellationToken);

        return endpoints.ToDictionary(
            endpoint => endpoint.CategoryId,
            endpoint => new EndpointRow(
                endpoint.CategoryId,
                endpoint.Name,
                [.. endpoint.AncestorIds.Select(id => ancestorNames[id]), endpoint.Name],
                endpoint.IsDeleted));
    }

    private async Task<long> CurrentGenerationAsync(CancellationToken cancellationToken) =>
        (await db.AbwabRevisionStates.AsNoTracking().SingleAsync(r => r.Id == AbwabRevisionState.SingletonId, cancellationToken))
            .TimelineGeneration;

    // Endpoint and ancestor rows are looked up by indexer, not TryGetValue with a placeholder: the
    // endpoint FKs are RESTRICT and AncestorIds is maintained by the 029 writer, so a miss means the
    // tree is corrupt. Failing there beats rendering an empty name or a raw GUID inside a door path.
    private static CategoryRelationshipDto ToDto(CategoryRelationship row, IReadOnlyDictionary<Guid, EndpointRow> endpoints)
    {
        var ids = row.EndpointCategoryIds;
        return new CategoryRelationshipDto(
            row.CategoryRelationshipId,
            row.RelationshipType,
            row.IsDirectional,
            ToEndpointDto(endpoints[ids[0]]),
            ToEndpointDto(endpoints[ids[1]]),
            row.IsDeleted,
            row.Version);
    }

    private static CategoryRelationshipEndpointDto ToEndpointDto(EndpointRow endpoint) =>
        new(endpoint.CategoryId, endpoint.Name, endpoint.Path, endpoint.IsDeleted);

    private sealed record EndpointRow(Guid CategoryId, string Name, IReadOnlyList<string> Path, bool IsDeleted);
}
