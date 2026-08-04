using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabRelationsWriter
{
    Task<IReadOnlyList<AbwabDoorRelationDto>> AddAsync(
        int doorId,
        AbwabRelationType type,
        AbwabRelationDirection? direction,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int relationId, CancellationToken cancellationToken);
}
