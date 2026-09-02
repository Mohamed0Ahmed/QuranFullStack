using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer(
    QuranDashboardDbContext db,
    LinkingWriteLockProtocol lockProtocol) : IAbwabDoorInclusionSynchronizer
{
    public async Task<IReadOnlyList<int>> SynchronizeAsync(
        int sourceDoorId,
        AbwabDoorInclusionMutationSet mutations,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutations);
        if (sourceDoorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceDoorId));
        }

        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actorUserId));
        }

        if (mutations.IsEmpty)
        {
            return [];
        }

        await lockProtocol.AcquireDoorInclusionGraphMutationAsync(cancellationToken);
        var traversal = await LoadActiveConsumerTraversalAsync(sourceDoorId, cancellationToken);
        if (traversal.Count == 0)
        {
            return [];
        }

        var edgeContexts = await LoadEdgeContextsAsync(traversal, cancellationToken);
        var mutationsByDoor = new Dictionary<int, DoorMutationAccumulator>
        {
            [sourceDoorId] = DoorMutationAccumulator.From(mutations),
        };
        var affectedAyahsByDoor = new Dictionary<int, HashSet<int>>();
        var deferredUnitIds = new HashSet<long>();
        var touchedContributionIds = new HashSet<long>();
        var now = DateTimeOffset.UtcNow;

        foreach (var edge in traversal)
        {
            if (!mutationsByDoor.TryGetValue(edge.SourceDoorId, out var sourceMutations)
                || sourceMutations.IsEmpty)
            {
                continue;
            }

            var context = edgeContexts[edge.InclusionId];
            var edgeMutations = sourceMutations.ToMutationSet();
            await ReconcileSourceUnitReplacementsAsync(
                context.Inclusion.Id,
                edgeMutations.Replacements,
                cancellationToken);

            var deletedTargetUnitIds = await DeleteSourceUnitsAsync(
                context,
                edgeMutations.DeletedUnitIds,
                actorUserId,
                now,
                affectedAyahsByDoor,
                deferredUnitIds,
                touchedContributionIds,
                cancellationToken);
            var editedSourceUnitIds = edgeMutations.EditedUnitIds
                .Concat(edgeMutations.Replacements.Select(replacement => replacement.CurrentUnitId))
                .Distinct()
                .Order()
                .ToArray();
            var editedTargetUnitIds = await EditSourceUnitsAsync(
                context,
                editedSourceUnitIds,
                actorUserId,
                now,
                affectedAyahsByDoor,
                touchedContributionIds,
                cancellationToken);
            var addedTargetUnitIds = await CloneUnitsAsync(
                context.Inclusion,
                context.Contribution,
                edgeMutations.AddedUnitIds,
                actorUserId,
                now,
                affectedAyahsByDoor,
                cancellationToken);

            if (addedTargetUnitIds.Count == 0
                && editedTargetUnitIds.Count == 0
                && deletedTargetUnitIds.Count == 0)
            {
                continue;
            }

            if (!mutationsByDoor.TryGetValue(edge.TargetDoorId, out var targetMutations))
            {
                targetMutations = new DoorMutationAccumulator();
                mutationsByDoor.Add(edge.TargetDoorId, targetMutations);
            }

            targetMutations.Add(
                addedTargetUnitIds,
                editedTargetUnitIds,
                deletedTargetUnitIds);
        }

        await DeleteDeferredUnitsAsync(deferredUnitIds, cancellationToken);
        await RefreshContributionsAsync(
            edgeContexts.Values,
            touchedContributionIds,
            actorUserId,
            now,
            cancellationToken);

        foreach (var affected in affectedAyahsByDoor.OrderBy(entry => entry.Key))
        {
            await new RelationalDoorStateRebuilder(db).RebuildAsync(
                affected.Key,
                affected.Value,
                actorUserId,
                true,
                cancellationToken);
        }

        var changedDoorIds = affectedAyahsByDoor.Keys.Order().ToArray();
        if (changedDoorIds.Length > 0)
        {
            var updatedDoorCount = await db.AbwabDoors
                .Where(door => changedDoorIds.Contains(door.Id))
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(door => door.UpdatedAtUtc, now)
                        .SetProperty(door => door.UpdatedBy, actorUserId),
                    cancellationToken);
            if (updatedDoorCount != changedDoorIds.Length)
            {
                throw new AbwabDoorInclusionSynchronizationConflictException();
            }
        }

        return changedDoorIds;
    }

    private async Task<IReadOnlyDictionary<int, AbwabDoorInclusionEdgeContext>> LoadEdgeContextsAsync(
        IReadOnlyList<AbwabDoorInclusionTraversalEdge> traversal,
        CancellationToken cancellationToken)
    {
        var inclusionIds = traversal.Select(edge => edge.InclusionId).Distinct().Order().ToArray();
        var inclusions = await db.AbwabDoorInclusions
            .Where(inclusion => inclusionIds.Contains(inclusion.Id) && inclusion.DeletedAtUtc == null)
            .OrderBy(inclusion => inclusion.Id)
            .ToListAsync(cancellationToken);
        var contributions = await db.LinkingSourceContributions
            .Where(contribution => contribution.DoorInclusionId != null
                && inclusionIds.Contains(contribution.DoorInclusionId.Value)
                && contribution.DeletedAtUtc == null)
            .OrderBy(contribution => contribution.DoorInclusionId)
            .ToListAsync(cancellationToken);
        if (inclusions.Count != inclusionIds.Length || contributions.Count != inclusionIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var contributionsByInclusionId = contributions.ToDictionary(
            contribution => contribution.DoorInclusionId!.Value);
        return inclusions.ToDictionary(
            inclusion => inclusion.Id,
            inclusion => new AbwabDoorInclusionEdgeContext(
                inclusion,
                contributionsByInclusionId[inclusion.Id]));
    }

    private async Task RefreshContributionsAsync(
        IEnumerable<AbwabDoorInclusionEdgeContext> contexts,
        IReadOnlySet<long> touchedContributionIds,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (touchedContributionIds.Count == 0)
        {
            return;
        }

        var contributionIds = touchedContributionIds.Order().ToArray();
        var ayahCounts = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking()
                    on mapping.UnitId equals unitAyah.UnitId
                where contributionIds.Contains(mapping.SourceContributionId)
                group unitAyah by mapping.SourceContributionId into grouped
                select new
                {
                    ContributionId = grouped.Key,
                    Count = grouped.Select(ayah => ayah.AyahId).Distinct().Count(),
                })
            .ToDictionaryAsync(row => row.ContributionId, row => row.Count, cancellationToken);
        var contributionsById = contexts
            .Select(context => context.Contribution)
            .DistinctBy(contribution => contribution.Id)
            .ToDictionary(contribution => contribution.Id);
        foreach (var contributionId in contributionIds)
        {
            var contribution = contributionsById[contributionId];
            contribution.ResolvedAyahCount = ayahCounts.GetValueOrDefault(contributionId);
            contribution.ResolvedAtUtc = now;
            contribution.UpdatedAtUtc = now;
            contribution.UpdatedBy = actorUserId;
        }

        await SaveChangesAsync(cancellationToken);
    }

    private sealed record AbwabDoorInclusionEdgeContext(
        AbwabDoorInclusion Inclusion,
        LinkingSourceContribution Contribution);

    private sealed class DoorMutationAccumulator
    {
        private readonly HashSet<long> _addedUnitIds = [];
        private readonly HashSet<long> _editedUnitIds = [];
        private readonly HashSet<long> _deletedUnitIds = [];
        private readonly List<AbwabDoorInclusionUnitReplacement> _replacements = [];

        public bool IsEmpty =>
            _addedUnitIds.Count == 0
            && _editedUnitIds.Count == 0
            && _deletedUnitIds.Count == 0
            && _replacements.Count == 0;

        public static DoorMutationAccumulator From(AbwabDoorInclusionMutationSet mutations)
        {
            var accumulator = new DoorMutationAccumulator();
            accumulator._addedUnitIds.UnionWith(mutations.AddedUnitIds);
            accumulator._editedUnitIds.UnionWith(mutations.EditedUnitIds);
            accumulator._deletedUnitIds.UnionWith(mutations.DeletedUnitIds);
            accumulator._replacements.AddRange(mutations.Replacements);
            return accumulator;
        }

        public void Add(
            IEnumerable<long> addedUnitIds,
            IEnumerable<long> editedUnitIds,
            IEnumerable<long> deletedUnitIds)
        {
            _addedUnitIds.UnionWith(addedUnitIds);
            _editedUnitIds.UnionWith(editedUnitIds);
            _deletedUnitIds.UnionWith(deletedUnitIds);
        }

        public AbwabDoorInclusionMutationSet ToMutationSet() =>
            AbwabDoorInclusionMutationSet.Create(
                _addedUnitIds,
                _editedUnitIds,
                _deletedUnitIds,
                _replacements);
    }
}
