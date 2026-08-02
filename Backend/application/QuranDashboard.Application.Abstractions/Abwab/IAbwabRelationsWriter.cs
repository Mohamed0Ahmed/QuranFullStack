using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabRelationsWriter
{
    // All-or-nothing: one refused target creates nothing. Throws AbwabNotFoundException (unknown
    // anchor or target), AbwabRelationArchivedDoorException, AbwabRelationSelfException, and
    // AbwabRelationDuplicateException.
    Task<IReadOnlyList<AbwabDoorRelationDto>> AddAsync(
        int doorId,
        AbwabRelationType type,
        AbwabRelationDirection? direction,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken);

    // False = relation missing or already deleted; deleting from either endpoint's modal hits the
    // same row, so no side is named here.
    Task<bool> DeleteAsync(int relationId, CancellationToken cancellationToken);
}
