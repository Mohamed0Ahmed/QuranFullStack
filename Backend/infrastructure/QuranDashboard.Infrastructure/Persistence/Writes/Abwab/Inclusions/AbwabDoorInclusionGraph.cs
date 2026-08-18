namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed class AbwabDoorInclusionGraph(
    IReadOnlyCollection<AbwabDoorInclusionGraph.Edge> activeEdges)
{
    private readonly Edge[] _activeEdges = activeEdges.Distinct().ToArray();

    public bool ContainsDirectEdge(int sourceDoorId, int targetDoorId) =>
        _activeEdges.Contains(new Edge(sourceDoorId, targetDoorId));

    public bool WouldCycle(int targetDoorId, IReadOnlyCollection<int> sourceDoorIds)
    {
        var edges = _activeEdges
            .Concat(sourceDoorIds.Select(sourceDoorId => new Edge(sourceDoorId, targetDoorId)))
            .Distinct()
            .ToArray();
        var doorIds = edges
            .SelectMany(edge => new[] { edge.SourceDoorId, edge.TargetDoorId })
            .Distinct()
            .ToArray();
        var indegree = doorIds.ToDictionary(doorId => doorId, _ => 0);
        var targetsBySource = edges
            .GroupBy(edge => edge.SourceDoorId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.TargetDoorId).Distinct().ToArray());

        foreach (var edge in edges)
        {
            indegree[edge.TargetDoorId]++;
        }

        var ready = new Queue<int>(indegree.Where(entry => entry.Value == 0).Select(entry => entry.Key));
        var visitedCount = 0;
        while (ready.TryDequeue(out var doorId))
        {
            visitedCount++;
            if (!targetsBySource.TryGetValue(doorId, out var targets))
            {
                continue;
            }

            foreach (var target in targets)
            {
                indegree[target]--;
                if (indegree[target] == 0)
                {
                    ready.Enqueue(target);
                }
            }
        }

        return visitedCount != doorIds.Length;
    }

    public IReadOnlyList<int> ReachableConsumersOf(int sourceDoorId)
    {
        var targetsBySource = _activeEdges
            .GroupBy(edge => edge.SourceDoorId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.TargetDoorId).Distinct().Order().ToArray());
        var discovered = new HashSet<int> { sourceDoorId };
        var pending = new Queue<int>();
        pending.Enqueue(sourceDoorId);

        while (pending.TryDequeue(out var doorId))
        {
            if (!targetsBySource.TryGetValue(doorId, out var targets))
            {
                continue;
            }

            foreach (var target in targets)
            {
                if (discovered.Add(target))
                {
                    pending.Enqueue(target);
                }
            }
        }

        discovered.Remove(sourceDoorId);
        return discovered.Order().ToArray();
    }

    internal sealed record Edge(int SourceDoorId, int TargetDoorId);
}
