using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer
{
    public async Task MarkTargetUnitOverriddenAsync(
        int targetDoorId,
        long targetUnitId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (targetDoorId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDoorId));
        }

        if (targetUnitId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetUnitId));
        }

        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(actorUserId));
        }

        var sync = await db.AbwabDoorInclusionUnitSyncs
            .SingleOrDefaultAsync(candidate => candidate.TargetUnitId == targetUnitId, cancellationToken);
        if (sync is null)
        {
            return;
        }

        if (sync.State is not AbwabDoorInclusionSyncState.Active
            and not AbwabDoorInclusionSyncState.Overridden)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var ownsTargetUnit = await (
                from inclusion in db.AbwabDoorInclusions.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on inclusion.Id equals contribution.DoorInclusionId
                join mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                    on contribution.Id equals mapping.SourceContributionId
                where inclusion.Id == sync.DoorInclusionId
                    && inclusion.TargetDoorId == targetDoorId
                    && inclusion.DeletedAtUtc == null
                    && contribution.DoorId == targetDoorId
                    && contribution.SourceKind == LinkingSourceKind.DoorInclusion
                    && contribution.DeletedAtUtc == null
                    && mapping.UnitId == targetUnitId
                select mapping.UnitId)
            .AnyAsync(cancellationToken);
        if (!ownsTargetUnit)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        sync.State = AbwabDoorInclusionSyncState.Overridden;
        sync.UpdatedAtUtc = DateTimeOffset.UtcNow;
        sync.UpdatedBy = actorUserId;
        await SaveChangesAsync(cancellationToken);
    }
}
