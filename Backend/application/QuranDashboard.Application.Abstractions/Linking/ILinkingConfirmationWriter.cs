using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingConfirmationWriter
{
    Task<LinkingConfirmationWriteResult> ConfirmPreparedAsync(
        LinkingConfirmationJobLease lease,
        CancellationToken cancellationToken);
}

public sealed record LinkingConfirmationRequestContract(
    string Kind,
    int SchemaVersion,
    string RequestHash,
    long LinkingDataRevision,
    Guid? PreparedPreflightReferenceId = null,
    Guid? ConfirmationJobReferenceId = null,
    Guid? PreparedPreflightId = null);

public abstract record LinkingConfirmationWriteResult
{
    private LinkingConfirmationWriteResult() { }

    public sealed record Success(
        LinkingConfirmationResultDto Result,
        bool IsReplay) : LinkingConfirmationWriteResult;

    public sealed record DoorNotFound(int DoorId) : LinkingConfirmationWriteResult;
}
