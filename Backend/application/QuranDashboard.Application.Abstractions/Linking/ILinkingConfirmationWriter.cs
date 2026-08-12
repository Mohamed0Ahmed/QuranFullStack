using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingConfirmationWriter
{
    Task<LinkingConfirmationWriteResult> ConfirmAsync(
        int actorUserId,
        LinkingOperationRequest request,
        LinkingOperationIntent intent,
        Func<LinkingOperationIntent, LinkingConfirmedDoorState, LinkingOperationClassification> classify,
        CancellationToken cancellationToken);
}

public abstract record LinkingConfirmationWriteResult
{
    private LinkingConfirmationWriteResult() { }

    public sealed record Success(
        LinkingConfirmationResultDto Result,
        bool IsReplay) : LinkingConfirmationWriteResult;

    public sealed record DoorNotFound(int DoorId) : LinkingConfirmationWriteResult;

    public sealed record InvalidClassification(
        LinkingOperationClassification Classification) : LinkingConfirmationWriteResult;
}
