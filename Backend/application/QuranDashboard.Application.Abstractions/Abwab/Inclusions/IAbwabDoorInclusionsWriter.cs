using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public interface IAbwabDoorInclusionsWriter
{
    Task<AbwabDoorInclusionAddWriteResult> AddAsync(
        int targetDoorId,
        uint expectedTargetDoorVersion,
        IReadOnlyList<int> sourceDoorIds,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<AbwabDoorInclusionDetachWriteResult> DetachAsync(
        int targetDoorId,
        int inclusionId,
        uint expectedTargetDoorVersion,
        int actorUserId,
        CancellationToken cancellationToken);
}
