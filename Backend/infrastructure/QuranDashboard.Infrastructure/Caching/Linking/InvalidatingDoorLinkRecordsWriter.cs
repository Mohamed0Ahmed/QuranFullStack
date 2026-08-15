using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Caching.Linking;

internal sealed class InvalidatingDoorLinkRecordsWriter(
    EfDoorLinkRecordsWriter inner,
    IAbwabCacheInvalidator invalidator) : IDoorLinkRecordsWriter
{
    public async Task<DoorLinkMutationWriteResult> ReplaceWordsAsync(
        int doorId,
        long unitId,
        uint expectedDoorVersion,
        IReadOnlyList<DoorLinkSelectedWord> selectedWords,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await inner.ReplaceWordsAsync(
            doorId,
            unitId,
            expectedDoorVersion,
            selectedWords,
            actorUserId,
            cancellationToken);
        InvalidateAfterCommittedChange(result);
        return result;
    }

    public async Task<DoorLinkMutationWriteResult> DeleteAsync(
        int doorId,
        uint expectedDoorVersion,
        DoorLinkSelection selection,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await inner.DeleteAsync(
            doorId,
            expectedDoorVersion,
            selection,
            actorUserId,
            cancellationToken);
        InvalidateAfterCommittedChange(result);
        return result;
    }

    private void InvalidateAfterCommittedChange(DoorLinkMutationWriteResult result)
    {
        if (result is DoorLinkMutationWriteResult.Success { IsNoOp: false })
        {
            invalidator.InvalidateTree();
        }
    }
}
