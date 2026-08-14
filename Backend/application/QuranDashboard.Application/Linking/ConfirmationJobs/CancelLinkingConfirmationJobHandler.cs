using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class CancelLinkingConfirmationJobHandler(ILinkingConfirmationJobStore store)
{
    public async Task<CancelLinkingConfirmationJobOutcome> HandleAsync(
        int actorUserId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await store.CancelAsync(actorUserId, jobId, cancellationToken);
            return status is null
                ? new CancelLinkingConfirmationJobOutcome.NotFound()
                : new CancelLinkingConfirmationJobOutcome.Success(status);
        }
        catch (LinkingConfirmationJobConflictException exception)
        {
            return new CancelLinkingConfirmationJobOutcome.Conflict(
                exception.Kind,
                exception.FailureCode);
        }
    }
}

public abstract record CancelLinkingConfirmationJobOutcome
{
    private CancelLinkingConfirmationJobOutcome() { }

    public sealed record Success(LinkingConfirmationJobStatusDto Status)
        : CancelLinkingConfirmationJobOutcome;

    public sealed record NotFound : CancelLinkingConfirmationJobOutcome;

    public sealed record Conflict(
        LinkingConfirmationJobConflictKind Kind,
        string FailureCode) : CancelLinkingConfirmationJobOutcome;
}
