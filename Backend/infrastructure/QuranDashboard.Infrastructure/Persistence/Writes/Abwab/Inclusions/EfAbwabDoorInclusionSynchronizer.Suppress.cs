using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer
{
    public async Task<IReadOnlyList<long>> PrepareTargetUnitSuppressionsAsync(
        int targetDoorId,
        IReadOnlyCollection<long> targetUnitIds,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetUnitIds);
        if (targetDoorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDoorId));
        }

        if (targetUnitIds.Any(targetUnitId => targetUnitId <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(targetUnitIds));
        }

        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actorUserId));
        }

        if (targetUnitIds.Count == 0)
        {
            return [];
        }

        var selectedUnitIds = targetUnitIds.Distinct().Order().ToArray();
        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.TargetUnitId != null && selectedUnitIds.Contains(sync.TargetUnitId.Value))
            .OrderBy(sync => sync.TargetUnitId)
            .ToListAsync(cancellationToken);
        if (syncs.Count == 0)
        {
            return [];
        }

        if (syncs.Any(sync => sync.State is not AbwabDoorInclusionSyncState.Active
                and not AbwabDoorInclusionSyncState.Overridden))
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var inclusionIds = syncs.Select(sync => sync.DoorInclusionId).Distinct().Order().ToArray();
        var inclusions = await db.AbwabDoorInclusions.AsNoTracking()
            .Where(inclusion => inclusionIds.Contains(inclusion.Id)
                && inclusion.TargetDoorId == targetDoorId
                && inclusion.DeletedAtUtc == null)
            .OrderBy(inclusion => inclusion.Id)
            .ToListAsync(cancellationToken);
        var contributions = await db.LinkingSourceContributions
            .Where(contribution => contribution.DoorInclusionId != null
                && inclusionIds.Contains(contribution.DoorInclusionId.Value)
                && contribution.DoorId == targetDoorId
                && contribution.SourceKind == LinkingSourceKind.DoorInclusion
                && contribution.DeletedAtUtc == null)
            .OrderBy(contribution => contribution.DoorInclusionId)
            .ToListAsync(cancellationToken);
        if (inclusions.Count != inclusionIds.Length
            || contributions.Count != inclusionIds.Length
            || contributions.Select(contribution => contribution.DoorInclusionId).Distinct().Count()
                != inclusionIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var contributionIdByInclusionId = contributions.ToDictionary(
            contribution => contribution.DoorInclusionId!.Value,
            contribution => contribution.Id);
        var synchronizedUnitIds = syncs.Select(sync => sync.TargetUnitId!.Value).Order().ToArray();
        var contributionIds = contributions.Select(contribution => contribution.Id).Order().ToArray();
        var mappings = await db.LinkingSourceContributionUnits
            .Where(mapping => contributionIds.Contains(mapping.SourceContributionId)
                && synchronizedUnitIds.Contains(mapping.UnitId))
            .OrderBy(mapping => mapping.SourceContributionId)
            .ThenBy(mapping => mapping.UnitId)
            .ToListAsync(cancellationToken);
        if (mappings.Count != synchronizedUnitIds.Length
            || mappings.Select(mapping => mapping.UnitId).Distinct().Count() != mappings.Count)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var mappingByUnitId = mappings.ToDictionary(mapping => mapping.UnitId);
        if (syncs.Any(sync =>
            !mappingByUnitId.TryGetValue(sync.TargetUnitId!.Value, out var mapping)
            || mapping.SourceContributionId != contributionIdByInclusionId[sync.DoorInclusionId]))
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var now = DateTimeOffset.UtcNow;
        db.LinkingSourceContributionUnits.RemoveRange(mappings);
        foreach (var sync in syncs)
        {
            sync.TargetUnitId = null;
            sync.State = AbwabDoorInclusionSyncState.Suppressed;
            sync.UpdatedAtUtc = now;
            sync.UpdatedBy = actorUserId;
        }

        await SaveChangesAsync(cancellationToken);

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
        foreach (var contribution in contributions)
        {
            contribution.ResolvedAyahCount = ayahCounts.GetValueOrDefault(contribution.Id);
            contribution.ResolvedAtUtc = now;
            contribution.UpdatedAtUtc = now;
            contribution.UpdatedBy = actorUserId;
        }

        await SaveChangesAsync(cancellationToken);
        return synchronizedUnitIds;
    }
}
