using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionsWriter
{
    public async Task<AbwabDoorInclusionDetachWriteResult> DetachAsync(
        int targetDoorId,
        int inclusionId,
        uint expectedTargetDoorVersion,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (targetDoorId <= 0 || inclusionId <= 0 || actorUserId <= 0)
        {
            return new AbwabDoorInclusionDetachWriteResult.InvalidRequest();
        }

        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await DetachWithinTransactionAsync(
                targetDoorId,
                inclusionId,
                expectedTargetDoorVersion,
                actorUserId,
                cancellationToken);
            if (result is AbwabDoorInclusionDetachWriteResult.Success)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionDetachWriteResult.StaleTargetVersion();
        }
        catch (AbwabDoorInclusionSynchronizationConflictException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionDetachWriteResult.SynchronizationUnavailable();
        }
        catch (AbwabDoorInclusionSynchronizationUnavailableException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionDetachWriteResult.SynchronizationUnavailable();
        }
        catch (LinkingStaleVersionException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionDetachWriteResult.SynchronizationUnavailable();
        }
    }

    private async Task<AbwabDoorInclusionDetachWriteResult> DetachWithinTransactionAsync(
        int targetDoorId,
        int inclusionId,
        uint expectedTargetDoorVersion,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await lockProtocol.AcquireDoorInclusionGraphMutationAsync(cancellationToken);

        var activeInclusions = await db.AbwabDoorInclusions.AsNoTracking()
            .Where(inclusion => inclusion.DeletedAtUtc == null)
            .Select(inclusion => new
            {
                inclusion.Id,
                inclusion.SourceDoorId,
                inclusion.TargetDoorId,
            })
            .ToListAsync(cancellationToken);
        var detachedInclusion = activeInclusions.SingleOrDefault(inclusion =>
            inclusion.Id == inclusionId && inclusion.TargetDoorId == targetDoorId);
        if (detachedInclusion is null)
        {
            return new AbwabDoorInclusionDetachWriteResult.NotFound();
        }

        var graph = new AbwabDoorInclusionGraph(activeInclusions.Select(inclusion =>
            new AbwabDoorInclusionGraph.Edge(inclusion.SourceDoorId, inclusion.TargetDoorId)).ToArray());
        var doorIdsToLock = graph.ReachableConsumersOf(targetDoorId)
            .Append(targetDoorId)
            .Distinct()
            .Order()
            .ToArray();
        var lockedDoors = await LockDoorsAsync(doorIdsToLock, cancellationToken);
        if (lockedDoors.Count != doorIdsToLock.Length)
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }

        var targetDoor = lockedDoors.Single(door => door.Id == targetDoorId);
        if (targetDoor.DeletedAtUtc is not null)
        {
            return new AbwabDoorInclusionDetachWriteResult.ArchivedTarget();
        }

        if (targetDoor.Version != expectedTargetDoorVersion)
        {
            return new AbwabDoorInclusionDetachWriteResult.StaleTargetVersion();
        }

        var inclusion = await db.AbwabDoorInclusions.SingleOrDefaultAsync(candidate =>
            candidate.Id == inclusionId
            && candidate.TargetDoorId == targetDoorId
            && candidate.DeletedAtUtc == null,
            cancellationToken);
        var contribution = await db.LinkingSourceContributions.SingleOrDefaultAsync(candidate =>
            candidate.DoorInclusionId == inclusionId
            && candidate.SourceKind == LinkingSourceKind.DoorInclusion
            && candidate.DeletedAtUtc == null,
            cancellationToken);
        if (inclusion is null || contribution is null || contribution.DoorId != targetDoorId)
        {
            throw new AbwabDoorInclusionSynchronizationUnavailableException();
        }

        var syncs = await db.AbwabDoorInclusionUnitSyncs
            .Where(sync => sync.DoorInclusionId == inclusionId)
            .OrderBy(sync => sync.SourceUnitId)
            .ToListAsync(cancellationToken);
        if (syncs.Any(sync => sync.State switch
            {
                AbwabDoorInclusionSyncState.Active => sync.TargetUnitId is null,
                AbwabDoorInclusionSyncState.Overridden => sync.TargetUnitId is null,
                AbwabDoorInclusionSyncState.Suppressed => sync.TargetUnitId is not null,
                _ => true,
            }))
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
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
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var snapshots = await AbwabDoorInclusionSourceSnapshot.LoadAsync(
            db,
            targetUnitIds,
            cancellationToken);
        if (snapshots.Count != targetUnitIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        await synchronizer.SynchronizeAsync(
            targetDoorId,
            AbwabDoorInclusionMutationSet.Create([], [], targetUnitIds, []),
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
        inclusion.UpdatedAtUtc = now;
        inclusion.UpdatedBy = actorUserId;
        inclusion.DeletedAtUtc = now;
        inclusion.DeletedBy = actorUserId;
        targetDoor.UpdatedAtUtc = now;
        targetDoor.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(cancellationToken);

        await DeleteDetachedUnitsAsync(targetUnitIds, cancellationToken);
        var affectedAyahIds = snapshots.Values
            .SelectMany(snapshot => snapshot.Ayahs)
            .Select(ayah => ayah.AyahId)
            .Distinct()
            .Order()
            .ToArray();
        await new RelationalDoorStateRebuilder(db).RebuildAsync(
            targetDoorId,
            affectedAyahIds,
            actorUserId,
            true,
            cancellationToken);

        return new AbwabDoorInclusionDetachWriteResult.Success(
            new AbwabDoorInclusionDetachResultDto(
                inclusionId,
                targetUnitIds.Length,
                targetDoor.Version));
    }

    private async Task DeleteDetachedUnitsAsync(
        IReadOnlyCollection<long> targetUnitIds,
        CancellationToken cancellationToken)
    {
        if (targetUnitIds.Count == 0)
        {
            return;
        }

        var ids = targetUnitIds.Distinct().Order().ToArray();
        var hasContributionMappings = await db.LinkingSourceContributionUnits.AsNoTracking()
            .AnyAsync(mapping => ids.Contains(mapping.UnitId), cancellationToken);
        var hasSyncMappings = await db.AbwabDoorInclusionUnitSyncs.AsNoTracking()
            .AnyAsync(sync => ids.Contains(sync.SourceUnitId)
                || (sync.TargetUnitId != null && ids.Contains(sync.TargetUnitId.Value)), cancellationToken);
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
