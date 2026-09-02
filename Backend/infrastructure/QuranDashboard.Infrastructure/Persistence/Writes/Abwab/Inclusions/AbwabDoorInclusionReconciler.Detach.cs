using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class AbwabDoorInclusionReconciler
{
    internal async Task<int> ReconcileDetachAsync(
        AbwabDoorInclusion inclusion,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var contribution = await db.LinkingSourceContributions.SingleOrDefaultAsync(candidate =>
            candidate.DoorInclusionId == inclusion.Id
            && candidate.SourceKind == LinkingSourceKind.DoorInclusion
            && candidate.DeletedAtUtc == null,
            cancellationToken);
        if (contribution is null || contribution.DoorId != inclusion.TargetDoorId)
        {
            throw new AbwabDoorInclusionReconciliationUnavailableException();
        }

        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.DoorInclusionId == inclusion.Id)
            .OrderBy(sync => sync.SourceUnitId)
            .ToListAsync(cancellationToken);
        if (HasInvalidSyncState(syncs))
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        var targetUnitIds = syncs
            .Where(sync => sync.TargetUnitId is not null)
            .Select(sync => sync.TargetUnitId!.Value)
            .Order()
            .ToArray();
        var mappings = await db.LinkingSourceContributionUnits
            .Where(mapping => mapping.SourceContributionId == contribution.Id)
            .OrderBy(mapping => mapping.UnitId)
            .ToListAsync(cancellationToken);
        if (!mappings.Select(mapping => mapping.UnitId).SequenceEqual(targetUnitIds))
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        var snapshots = await SourceSnapshot.LoadAsync(db, targetUnitIds, cancellationToken);
        if (snapshots.Count != targetUnitIds.Length)
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        await ReconcileSourceChangeAsync(
            inclusion.TargetDoorId,
            [],
            [],
            targetUnitIds,
            [],
            actorUserId,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        db.LinkingSourceContributionUnits.RemoveRange(mappings);
        db.AbwabDoorInclusionUnitSyncs.RemoveRange(syncs);
        contribution.ResolvedAyahCount = 0;
        contribution.ResolvedAtUtc = now;
        contribution.UpdatedAtUtc = now;
        contribution.UpdatedBy = actorUserId;
        contribution.DeletedAtUtc = now;
        contribution.DeletedBy = actorUserId;
        await SaveChangesAsync(cancellationToken);

        await DeleteDeferredUnitsAsync(targetUnitIds.ToHashSet(), cancellationToken);
        await new RelationalDoorStateRebuilder(db).RebuildAsync(
            inclusion.TargetDoorId,
            snapshots.Values.SelectMany(snapshot => snapshot.Ayahs)
                .Select(ayah => ayah.AyahId)
                .Distinct()
                .Order()
                .ToArray(),
            actorUserId,
            true,
            cancellationToken);

        return targetUnitIds.Length;
    }
}
