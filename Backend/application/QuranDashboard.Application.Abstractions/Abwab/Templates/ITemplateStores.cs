using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public interface IDoorTemplateStore
{
    Task<DoorTemplate?> FindTrackedAsync(Guid doorTemplateId, CancellationToken cancellationToken);

    void Add(DoorTemplate template);
}

public interface ITemplateNodeStore
{
    Task<TemplateNode?> FindTrackedAsync(Guid templateNodeId, CancellationToken cancellationToken);

    // The whole template's nodes, tracked: reparent/reorder validate the parent chain and rewrite
    // sibling orders over one in-transaction read rather than a query per level.
    Task<IReadOnlyList<TemplateNode>> FindTrackedForTemplateAsync(Guid doorTemplateId, CancellationToken cancellationToken);

    void Add(TemplateNode node);
}

public interface ITemplateNodeAliasStore
{
    Task<TemplateNodeSearchAlias?> FindTrackedAsync(Guid templateNodeSearchAliasId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TemplateNodeSearchAlias>> FindTrackedForNodesAsync(
        IReadOnlyCollection<Guid> templateNodeIds,
        CancellationToken cancellationToken);

    void Add(TemplateNodeSearchAlias alias);
}
