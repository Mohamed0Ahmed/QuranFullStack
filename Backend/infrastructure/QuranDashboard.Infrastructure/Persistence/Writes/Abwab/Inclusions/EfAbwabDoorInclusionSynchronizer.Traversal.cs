namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer
{
    private const int TraversalBatchSize = 500;

    private async Task<IReadOnlyList<AbwabDoorInclusionTraversalEdge>> LoadActiveConsumerTraversalAsync(
        int sourceDoorId,
        CancellationToken cancellationToken)
    {
        var pendingSourceDoorIds = new Queue<int>();
        var discoveredDoorIds = new HashSet<int> { sourceDoorId };
        var edgesById = new Dictionary<int, AbwabDoorInclusionTraversalEdge>();
        pendingSourceDoorIds.Enqueue(sourceDoorId);

        while (pendingSourceDoorIds.Count > 0)
        {
            var sourceDoorIds = new List<int>(TraversalBatchSize);
            while (sourceDoorIds.Count < TraversalBatchSize && pendingSourceDoorIds.TryDequeue(out var doorId))
            {
                sourceDoorIds.Add(doorId);
            }

            var edges = await db.AbwabDoorInclusions.AsNoTracking()
                .Where(inclusion =>
                    inclusion.DeletedAtUtc == null
                    && sourceDoorIds.Contains(inclusion.SourceDoorId))
                .OrderBy(inclusion => inclusion.SourceDoorId)
                .ThenBy(inclusion => inclusion.TargetDoorId)
                .ThenBy(inclusion => inclusion.Id)
                .Select(inclusion => new AbwabDoorInclusionTraversalEdge(
                    inclusion.Id,
                    inclusion.SourceDoorId,
                    inclusion.TargetDoorId,
                    0))
                .ToListAsync(cancellationToken);

            foreach (var edge in edges)
            {
                edgesById.Add(edge.InclusionId, edge);
                if (discoveredDoorIds.Add(edge.TargetDoorId))
                {
                    pendingSourceDoorIds.Enqueue(edge.TargetDoorId);
                }
            }
        }

        return OrderTraversal(edgesById.Values, discoveredDoorIds);
    }

    private static IReadOnlyList<AbwabDoorInclusionTraversalEdge> OrderTraversal(
        IEnumerable<AbwabDoorInclusionTraversalEdge> edges,
        IReadOnlyCollection<int> doorIds)
    {
        var edgeList = edges.ToArray();
        if (edgeList.Length == 0)
        {
            return [];
        }

        var indegree = doorIds.ToDictionary(doorId => doorId, _ => 0);
        var targetsBySource = edgeList
            .GroupBy(edge => edge.SourceDoorId)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.TargetDoorId).Distinct().ToArray());
        foreach (var edge in edgeList)
        {
            indegree[edge.TargetDoorId]++;
        }

        var ready = new SortedSet<int>(indegree.Where(entry => entry.Value == 0).Select(entry => entry.Key));
        var orderByDoorId = new Dictionary<int, int>();
        var sequence = 0;
        while (ready.Count > 0)
        {
            var doorId = ready.Min;
            ready.Remove(doorId);
            orderByDoorId.Add(doorId, sequence++);

            if (!targetsBySource.TryGetValue(doorId, out var targets))
            {
                continue;
            }

            foreach (var targetDoorId in targets)
            {
                indegree[targetDoorId]--;
                if (indegree[targetDoorId] == 0)
                {
                    ready.Add(targetDoorId);
                }
            }
        }

        if (orderByDoorId.Count != doorIds.Count)
        {
            throw new InvalidOperationException("The active door inclusion graph contains a cycle.");
        }

        return edgeList
            .OrderBy(edge => orderByDoorId[edge.TargetDoorId])
            .ThenBy(edge => orderByDoorId[edge.SourceDoorId])
            .ThenBy(edge => edge.InclusionId)
            .Select(edge => edge with { Sequence = orderByDoorId[edge.TargetDoorId] })
            .ToArray();
    }

    private sealed record AbwabDoorInclusionTraversalEdge(
        int InclusionId,
        int SourceDoorId,
        int TargetDoorId,
        int Sequence);
}
