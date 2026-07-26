using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

public sealed class EfDoorTemplateStore(QuranDashboardDbContext db) : IDoorTemplateStore
{
    public Task<DoorTemplate?> FindTrackedAsync(Guid doorTemplateId, CancellationToken cancellationToken) =>
        db.AbwabDoorTemplates.SingleOrDefaultAsync(t => t.DoorTemplateId == doorTemplateId, cancellationToken);

    public void Add(DoorTemplate template) => db.AbwabDoorTemplates.Add(template);
}

public sealed class EfTemplateNodeStore(QuranDashboardDbContext db) : ITemplateNodeStore
{
    public Task<TemplateNode?> FindTrackedAsync(Guid templateNodeId, CancellationToken cancellationToken) =>
        db.AbwabTemplateNodes.SingleOrDefaultAsync(n => n.TemplateNodeId == templateNodeId, cancellationToken);

    public async Task<IReadOnlyList<TemplateNode>> FindTrackedForTemplateAsync(Guid doorTemplateId, CancellationToken cancellationToken) =>
        await db.AbwabTemplateNodes
            .Where(n => n.DoorTemplateId == doorTemplateId)
            .OrderBy(n => n.SiblingOrder)
            .ToListAsync(cancellationToken);

    public void Add(TemplateNode node) => db.AbwabTemplateNodes.Add(node);
}

public sealed class EfTemplateNodeAliasStore(QuranDashboardDbContext db) : ITemplateNodeAliasStore
{
    public Task<TemplateNodeSearchAlias?> FindTrackedAsync(Guid templateNodeSearchAliasId, CancellationToken cancellationToken) =>
        db.AbwabTemplateNodeSearchAliases.SingleOrDefaultAsync(a => a.TemplateNodeSearchAliasId == templateNodeSearchAliasId, cancellationToken);

    public async Task<IReadOnlyList<TemplateNodeSearchAlias>> FindTrackedForNodesAsync(
        IReadOnlyCollection<Guid> templateNodeIds,
        CancellationToken cancellationToken) =>
        templateNodeIds.Count == 0
            ? []
            : await db.AbwabTemplateNodeSearchAliases
                .Where(a => templateNodeIds.Contains(a.TemplateNodeId))
                .ToListAsync(cancellationToken);

    public void Add(TemplateNodeSearchAlias alias) => db.AbwabTemplateNodeSearchAliases.Add(alias);
}
