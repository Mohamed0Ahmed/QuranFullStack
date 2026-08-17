using QuranDashboard.Application.Abstractions.Abwab.Inclusions;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionSynchronizer(
    QuranDashboardDbContext db,
    AbwabDoorInclusionSyncLock syncLock) : IAbwabDoorInclusionSynchronizer
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

        await syncLock.TakeAfterGlobalLocksBeforeDoorAndUnitLocksAsync(cancellationToken);
        var traversal = await LoadActiveConsumerTraversalAsync(sourceDoorId, cancellationToken);
        if (traversal.Count == 0)
        {
            return [];
        }

        throw new InvalidOperationException(
            "Door inclusion propagation is unavailable until its source-mutation phase is implemented.");
    }
}
