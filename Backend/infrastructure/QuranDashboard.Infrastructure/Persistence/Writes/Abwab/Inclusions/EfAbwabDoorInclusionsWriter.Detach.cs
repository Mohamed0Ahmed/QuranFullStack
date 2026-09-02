using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Linking;

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
        catch (AbwabDoorInclusionReconciliationConflictException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionDetachWriteResult.SynchronizationUnavailable();
        }
        catch (AbwabDoorInclusionReconciliationUnavailableException)
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
            throw new AbwabDoorInclusionReconciliationUnavailableException();
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
        if (inclusion is null)
        {
            throw new AbwabDoorInclusionReconciliationUnavailableException();
        }

        var removedUnitCount = await reconciler.ReconcileDetachAsync(
            inclusion,
            actorUserId,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        inclusion.UpdatedAtUtc = now;
        inclusion.UpdatedBy = actorUserId;
        inclusion.DeletedAtUtc = now;
        inclusion.DeletedBy = actorUserId;
        targetDoor.UpdatedAtUtc = now;
        targetDoor.UpdatedBy = actorUserId;
        await db.SaveChangesAsync(cancellationToken);

        return new AbwabDoorInclusionDetachWriteResult.Success(
            new AbwabDoorInclusionDetachResultDto(
                inclusionId,
                removedUnitCount,
                targetDoor.Version));
    }
}
