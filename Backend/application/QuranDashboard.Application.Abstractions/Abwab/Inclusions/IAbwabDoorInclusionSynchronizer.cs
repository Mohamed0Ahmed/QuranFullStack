namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public interface IAbwabDoorInclusionSynchronizer
{
    Task<IReadOnlyList<int>> SynchronizeAsync(
        int sourceDoorId,
        AbwabDoorInclusionMutationSet mutations,
        int actorUserId,
        CancellationToken cancellationToken);
}
