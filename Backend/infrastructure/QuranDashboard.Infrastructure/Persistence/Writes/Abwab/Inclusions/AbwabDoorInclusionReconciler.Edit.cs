using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class AbwabDoorInclusionReconciler
{
    private async Task<IReadOnlyList<long>> EditSourceUnitsAsync(
        AbwabDoorInclusionEdgeContext context,
        IReadOnlyCollection<long> sourceUnitIds,
        int actorUserId,
        DateTimeOffset now,
        IDictionary<int, HashSet<int>> affectedAyahsByDoor,
        ISet<long> touchedContributionIds,
        CancellationToken cancellationToken)
    {
        if (sourceUnitIds.Count == 0)
        {
            return [];
        }

        var orderedSourceUnitIds = sourceUnitIds.Distinct().Order().ToArray();
        var snapshots = await SourceSnapshot.LoadAsync(
            db,
            orderedSourceUnitIds,
            cancellationToken);
        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.DoorInclusionId == context.Inclusion.Id
                && orderedSourceUnitIds.Contains(sync.SourceUnitId))
            .OrderBy(sync => sync.SourceUnitId)
            .ToListAsync(cancellationToken);
        if (snapshots.Count != orderedSourceUnitIds.Length || syncs.Count != orderedSourceUnitIds.Length)
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        var changedActiveSyncs = new List<AbwabDoorInclusionUnitSync>();
        foreach (var sync in syncs)
        {
            var fingerprint = SourceFingerprint.Compute(snapshots[sync.SourceUnitId]);
            if (sync.SourceFingerprint.AsSpan().SequenceEqual(fingerprint))
            {
                continue;
            }

            sync.SourceFingerprint = fingerprint;
            sync.UpdatedAtUtc = now;
            sync.UpdatedBy = actorUserId;
            if (sync.State == AbwabDoorInclusionSyncState.Active)
            {
                if (sync.TargetUnitId is null)
                {
                    throw new AbwabDoorInclusionReconciliationConflictException();
                }

                changedActiveSyncs.Add(sync);
            }
        }

        if (changedActiveSyncs.Count == 0)
        {
            await SaveChangesAsync(cancellationToken);
            return [];
        }

        var targetUnitIds = changedActiveSyncs.Select(sync => sync.TargetUnitId!.Value).Order().ToArray();
        var previousTargetSnapshots = await SourceSnapshot.LoadAsync(
            db,
            targetUnitIds,
            cancellationToken);
        var targetUnits = await db.LinkingUnits
            .Where(unit => targetUnitIds.Contains(unit.Id))
            .OrderBy(unit => unit.Id)
            .ToListAsync(cancellationToken);
        if (previousTargetSnapshots.Count != targetUnitIds.Length || targetUnits.Count != targetUnitIds.Length)
        {
            throw new AbwabDoorInclusionReconciliationConflictException();
        }

        var sourceUnitIdByTargetUnitId = changedActiveSyncs.ToDictionary(
            sync => sync.TargetUnitId!.Value,
            sync => sync.SourceUnitId);
        foreach (var targetUnit in targetUnits)
        {
            targetUnit.IsGrouped = snapshots[sourceUnitIdByTargetUnitId[targetUnit.Id]].IsGrouped;
        }

        var targetUnitAyahIds = await db.LinkingUnitAyahs.AsNoTracking()
            .Where(ayah => targetUnitIds.Contains(ayah.UnitId))
            .Select(ayah => ayah.Id)
            .ToListAsync(cancellationToken);
        await db.LinkingUnitAyahDescriptions
            .Where(description => targetUnitAyahIds.Contains(description.UnitAyahId))
            .ExecuteDeleteAsync(cancellationToken);
        await db.LinkingUnitAyahWords
            .Where(word => targetUnitAyahIds.Contains(word.UnitAyahId))
            .ExecuteDeleteAsync(cancellationToken);
        await db.LinkingUnitAyahs
            .Where(ayah => targetUnitIds.Contains(ayah.UnitId))
            .ExecuteDeleteAsync(cancellationToken);

        var targetUnitsBySourceUnitId = targetUnits.ToDictionary(
            unit => sourceUnitIdByTargetUnitId[unit.Id],
            unit => unit);
        await AddCloneShapesAsync(
            targetUnitsBySourceUnitId,
            snapshots,
            actorUserId,
            now,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);

        AddAffectedAyahs(
            affectedAyahsByDoor,
            context.Inclusion.TargetDoorId,
            previousTargetSnapshots.Values.SelectMany(snapshot => snapshot.Ayahs).Select(ayah => ayah.AyahId));
        AddAffectedAyahs(
            affectedAyahsByDoor,
            context.Inclusion.TargetDoorId,
            changedActiveSyncs
                .Select(sync => snapshots[sync.SourceUnitId])
                .SelectMany(snapshot => snapshot.Ayahs)
                .Select(ayah => ayah.AyahId));
        touchedContributionIds.Add(context.Contribution.Id);
        return targetUnitIds;
    }

    private static void AddAffectedAyahs(
        IDictionary<int, HashSet<int>> affectedAyahsByDoor,
        int doorId,
        IEnumerable<int> ayahIds)
    {
        if (!affectedAyahsByDoor.TryGetValue(doorId, out var affectedAyahs))
        {
            affectedAyahs = [];
            affectedAyahsByDoor.Add(doorId, affectedAyahs);
        }

        affectedAyahs.UnionWith(ayahIds);
    }
}
