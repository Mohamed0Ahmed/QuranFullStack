using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public abstract record AbwabDoorInclusionAddWriteResult
{
    private AbwabDoorInclusionAddWriteResult() { }

    public sealed record Success(AbwabDoorInclusionAddResultDto Result) : AbwabDoorInclusionAddWriteResult;

    public sealed record InvalidRequest : AbwabDoorInclusionAddWriteResult;

    public sealed record NotFound : AbwabDoorInclusionAddWriteResult;

    public sealed record ArchivedDoor : AbwabDoorInclusionAddWriteResult;

    public sealed record Duplicate : AbwabDoorInclusionAddWriteResult;

    public sealed record Cycle : AbwabDoorInclusionAddWriteResult;

    public sealed record StaleTargetVersion : AbwabDoorInclusionAddWriteResult;

    public sealed record SynchronizationUnavailable : AbwabDoorInclusionAddWriteResult;
}

public sealed record AbwabDoorInclusionDetachResultDto(
    int InclusionId,
    int RemovedSynchronizedRecordCount,
    uint TargetDoorVersion);

public abstract record AbwabDoorInclusionDetachWriteResult
{
    private AbwabDoorInclusionDetachWriteResult() { }

    public sealed record Success(AbwabDoorInclusionDetachResultDto Result)
        : AbwabDoorInclusionDetachWriteResult;

    public sealed record InvalidRequest : AbwabDoorInclusionDetachWriteResult;

    public sealed record NotFound : AbwabDoorInclusionDetachWriteResult;

    public sealed record ArchivedTarget : AbwabDoorInclusionDetachWriteResult;

    public sealed record StaleTargetVersion : AbwabDoorInclusionDetachWriteResult;

    public sealed record SynchronizationUnavailable : AbwabDoorInclusionDetachWriteResult;
}
