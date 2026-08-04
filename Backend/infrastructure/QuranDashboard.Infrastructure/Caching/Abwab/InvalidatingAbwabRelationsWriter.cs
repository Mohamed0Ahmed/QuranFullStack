using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class InvalidatingAbwabRelationsWriter(
    EfAbwabRelationsWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabRelationsWriter
{
    private readonly EfAbwabRelationsWriter _inner = inner;
    private readonly IAbwabCacheInvalidator _invalidator = invalidator;

    public async Task<IReadOnlyList<AbwabDoorRelationDto>> AddAsync(
        int doorId,
        AbwabRelationType type,
        AbwabRelationDirection? direction,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.AddAsync(doorId, type, direction, targetDoorIds, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }

    public async Task<bool> DeleteAsync(int relationId, CancellationToken cancellationToken)
    {
        try
        {
            return await _inner.DeleteAsync(relationId, cancellationToken);
        }
        finally
        {
            _invalidator.InvalidateTree();
        }
    }
}
