using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Domain.Abwab.Categories;

namespace QuranDashboard.Application.Abwab.Relationships;

// Reads the denormalized AncestorIds rather than walking parent links, so building an audit
// payload stays a constant number of queries regardless of tree depth.
internal sealed class RelationshipEndpointViewBuilder(ICategoryTreeStore categories, ISectionWriteStore sections)
{
    public async Task<IReadOnlyDictionary<Guid, RelationshipEndpointAuditView>> BuildAsync(
        IReadOnlyCollection<Guid> endpointCategoryIds,
        CancellationToken cancellationToken)
    {
        var endpoints = await categories.FindManyTrackedAsync(endpointCategoryIds, cancellationToken);

        var ancestorIds = endpoints.SelectMany(endpoint => endpoint.AncestorIds).Distinct().ToList();
        var ancestors = ancestorIds.Count == 0
            ? []
            : await categories.FindManyTrackedAsync(ancestorIds, cancellationToken);
        var ancestorsById = ancestors.ToDictionary(ancestor => ancestor.CategoryId);

        var sectionIds = endpoints
            .Select(endpoint => SectionIdOf(endpoint, ancestorsById))
            .Where(sectionId => sectionId is not null)
            .Select(sectionId => sectionId!.Value)
            .Distinct()
            .ToList();
        var sectionNames = sectionIds.Count == 0
            ? []
            : (await sections.FindManyTrackedAsync(sectionIds, cancellationToken)).ToDictionary(s => s.SectionId, s => s.Name);

        return endpoints.ToDictionary(
            endpoint => endpoint.CategoryId,
            endpoint => new RelationshipEndpointAuditView(
                endpoint.CategoryId,
                endpoint.Name,
                SectionIdOf(endpoint, ancestorsById) is { } sectionId && sectionNames.TryGetValue(sectionId, out var name) ? name : null,
                // Indexer, not a GUID fallback: AncestorIds is maintained by the 029 writer, so a miss
                // means a corrupt tree and must not be papered over with a raw id inside a door path.
                [.. endpoint.AncestorIds.Select(id => ancestorsById[id].Name), endpoint.Name]));
    }

    private static Guid? SectionIdOf(Category endpoint, IReadOnlyDictionary<Guid, Category> ancestorsById)
    {
        if (endpoint.SectionId is { } ownSectionId)
        {
            return ownSectionId;
        }

        var rootId = endpoint.AncestorIds.Count > 0 ? endpoint.AncestorIds[0] : (Guid?)null;
        return rootId is { } id && ancestorsById.TryGetValue(id, out var root) ? root.SectionId : null;
    }
}
