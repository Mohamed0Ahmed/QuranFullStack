using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class GetLinkingConfirmationOutcomeHandler(ILinkingConfirmationJobStore store)
{
    public async Task<GetLinkingConfirmationOutcome> HandleAsync(
        int actorUserId,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await store.GetDurableOutcomeAsync(
                actorUserId,
                idempotencyKey,
                cancellationToken);
            return outcome is null
                ? new GetLinkingConfirmationOutcome.NotFound()
                : new GetLinkingConfirmationOutcome.Success(outcome);
        }
        catch (LinkingConfirmationJobConflictException exception)
        {
            return new GetLinkingConfirmationOutcome.Conflict(exception.FailureCode);
        }
    }
}

public abstract record GetLinkingConfirmationOutcome
{
    private GetLinkingConfirmationOutcome() { }

    public sealed record Success(LinkingDurableConfirmationOutcomeDto Outcome)
        : GetLinkingConfirmationOutcome;

    public sealed record NotFound : GetLinkingConfirmationOutcome;

    public sealed record Conflict(string FailureCode) : GetLinkingConfirmationOutcome;
}
