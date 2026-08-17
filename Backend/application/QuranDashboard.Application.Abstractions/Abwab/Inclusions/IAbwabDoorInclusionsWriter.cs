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
}
