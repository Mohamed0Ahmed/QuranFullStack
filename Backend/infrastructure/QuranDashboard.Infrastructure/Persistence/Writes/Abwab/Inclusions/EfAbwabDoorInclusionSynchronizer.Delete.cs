using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer
{
    private async Task<IReadOnlyList<long>> DeleteSourceUnitsAsync(
        AbwabDoorInclusionEdgeContext context,
        IReadOnlyCollection<long> sourceUnitIds,
        int actorUserId,
        DateTimeOffset now,
        IDictionary<int, HashSet<int>> affectedAyahsByDoor,
        ISet<long> deferredUnitIds,
        ISet<long> touchedContributionIds,
        CancellationToken cancellationToken)
    {
        if (sourceUnitIds.Count == 0)
        {
            return [];
        }

        var orderedSourceUnitIds = sourceUnitIds.Distinct().Order().ToArray();
        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.DoorInclusionId == context.Inclusion.Id
                && orderedSourceUnitIds.Contains(sync.SourceUnitId))
            .OrderBy(sync => sync.SourceUnitId)
            .ToListAsync(cancellationToken);
        if (syncs.Count != orderedSourceUnitIds.Length
            || syncs.Any(sync => sync.State != AbwabDoorInclusionSyncState.Active || sync.TargetUnitId is null))
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }

        var targetUnitIds = syncs.Select(sync => sync.TargetUnitId!.Value).Order().ToArray();
        var targetSnapshots = await AbwabDoorInclusionSourceSnapshot.LoadAsync(
            db,
            targetUnitIds,
            cancellationToken);
        var contributionMappings = await db.LinkingSourceContributionUnits
            .Where(mapping => mapping.SourceContributionId == context.Contribution.Id
                && targetUnitIds.Contains(mapping.UnitId))
            .OrderBy(mapping => mapping.UnitId)
            .ToListAsync(cancellationToken);
        if (targetSnapshots.Count != targetUnitIds.Length || contributionMappings.Count != targetUnitIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        db.LinkingSourceContributionUnits.RemoveRange(contributionMappings);
        db.AbwabDoorInclusionUnitSyncs.RemoveRange(syncs);
        context.Contribution.UpdatedAtUtc = now;
        context.Contribution.UpdatedBy = actorUserId;
        await SaveChangesAsync(cancellationToken);

        deferredUnitIds.UnionWith(targetUnitIds);
        touchedContributionIds.Add(context.Contribution.Id);
        AddAffectedAyahs(
            affectedAyahsByDoor,
            context.Inclusion.TargetDoorId,
            targetSnapshots.Values.SelectMany(snapshot => snapshot.Ayahs).Select(ayah => ayah.AyahId));
        return targetUnitIds;
    }

    private async Task DeleteDeferredUnitsAsync(
        IReadOnlySet<long> unitIds,
        CancellationToken cancellationToken)
    {
        if (unitIds.Count == 0)
        {
            return;
        }

        var ids = unitIds.Order().ToArray();
        var hasContributionMappings = await db.LinkingSourceContributionUnits.AsNoTracking()
            .AnyAsync(mapping => ids.Contains(mapping.UnitId), cancellationToken);
        var hasSyncMappings = await db.AbwabDoorInclusionUnitSyncs.AsNoTracking()
            .AnyAsync(
                sync => ids.Contains(sync.SourceUnitId)
                    || (sync.TargetUnitId != null && ids.Contains(sync.TargetUnitId.Value)),
                cancellationToken);
        if (hasContributionMappings || hasSyncMappings)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM linking_unit_ayah_descriptions
            WHERE unit_ayah_id IN (
                SELECT id FROM linking_unit_ayahs WHERE unit_id = ANY ({ids}));

            DELETE FROM linking_unit_ayah_words
            WHERE unit_ayah_id IN (
                SELECT id FROM linking_unit_ayahs WHERE unit_id = ANY ({ids}));

            DELETE FROM linking_unit_ayahs
            WHERE unit_id = ANY ({ids});

            DELETE FROM linking_units
            WHERE id = ANY ({ids});
            """,
            cancellationToken);
    }
}
