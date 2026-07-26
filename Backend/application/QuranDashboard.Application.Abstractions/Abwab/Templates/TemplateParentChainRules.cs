namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

// §7.4's "no cyclic template can be saved, applied, rendered, or restored" has two enforcement points
// — the node writer (Application) and the restore adapter (Infrastructure) — and must stay one rule:
// a walk that drifted on either side would let the other path persist a structure it rejects.
public static class TemplateParentChainRules
{
    public static Guid? FindCycleNodeId(IEnumerable<(Guid NodeId, Guid? ParentNodeId)> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var parentById = nodes.ToDictionary(node => node.NodeId, node => node.ParentNodeId);

        foreach (var startId in parentById.Keys)
        {
            var visited = new HashSet<Guid>();
            var currentId = (Guid?)startId;

            while (currentId is { } id && parentById.TryGetValue(id, out var parentId))
            {
                if (!visited.Add(id))
                {
                    return id;
                }

                currentId = parentId;
            }
        }

        return null;
    }
}
