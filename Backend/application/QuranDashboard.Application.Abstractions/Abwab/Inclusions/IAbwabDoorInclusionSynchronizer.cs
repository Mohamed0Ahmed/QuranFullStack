namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public interface IAbwabDoorInclusionSynchronizer
{
    Task MarkTargetUnitOverriddenAsync(
        int targetDoorId,
        long targetUnitId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<long>> PrepareTargetUnitSuppressionsAsync(
        int targetDoorId,
        IReadOnlyCollection<long> targetUnitIds,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<int>> SynchronizeAsync(
        int sourceDoorId,
        AbwabDoorInclusionMutationSet mutations,
        int actorUserId,
        CancellationToken cancellationToken);
}

public sealed class AbwabDoorInclusionSynchronizationConflictException : Exception
{
}

public sealed class AbwabDoorInclusionSynchronizationUnavailableException : Exception
{
}
