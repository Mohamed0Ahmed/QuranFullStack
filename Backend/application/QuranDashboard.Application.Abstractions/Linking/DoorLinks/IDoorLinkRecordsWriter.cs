namespace QuranDashboard.Application.Abstractions.Linking.DoorLinks;

public interface IDoorLinkRecordsWriter
{
    Task<DoorLinkMutationWriteResult> ReplaceWordsAsync(
        int doorId,
        long unitId,
        uint expectedDoorVersion,
        IReadOnlyList<DoorLinkSelectedWord> selectedWords,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<DoorLinkMutationWriteResult> DeleteAsync(
        int doorId,
        uint expectedDoorVersion,
        DoorLinkSelection selection,
        int actorUserId,
        CancellationToken cancellationToken);
}

public sealed record DoorLinkSelectedWord(int AyahId, int QuranWordId);

public sealed record DoorLinkMutationDto(int AffectedCount, uint DoorVersion);

public abstract record DoorLinkMutationWriteResult
{
    private DoorLinkMutationWriteResult() { }

    public sealed record Success(DoorLinkMutationDto Result, bool IsNoOp)
        : DoorLinkMutationWriteResult;

    public sealed record DoorNotFound : DoorLinkMutationWriteResult;
    public sealed record UnitNotFound : DoorLinkMutationWriteResult;
    public sealed record DoorArchived : DoorLinkMutationWriteResult;
    public sealed record DoorVersionStale : DoorLinkMutationWriteResult;
    public sealed record InvalidWords : DoorLinkMutationWriteResult;
    public sealed record SynchronizationUnavailable : DoorLinkMutationWriteResult;
}
