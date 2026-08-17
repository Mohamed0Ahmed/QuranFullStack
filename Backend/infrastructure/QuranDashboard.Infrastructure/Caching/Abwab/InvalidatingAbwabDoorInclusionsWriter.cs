using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

namespace QuranDashboard.Infrastructure.Caching.Abwab;

internal sealed class InvalidatingAbwabDoorInclusionsWriter(
    EfAbwabDoorInclusionsWriter inner,
    IAbwabCacheInvalidator invalidator) : IAbwabDoorInclusionsWriter
{
    public async Task<AbwabDoorInclusionAddWriteResult> AddAsync(
        int targetDoorId,
        uint expectedTargetDoorVersion,
        IReadOnlyList<int> sourceDoorIds,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await inner.AddAsync(
            targetDoorId,
            expectedTargetDoorVersion,
            sourceDoorIds,
            actorUserId,
            cancellationToken);
        if (result is AbwabDoorInclusionAddWriteResult.Success)
        {
            invalidator.InvalidateTree();
        }

        return result;
    }
}
