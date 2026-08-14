using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;

namespace QuranDashboard.Application.Linking.ConfirmationJobs;

public sealed class CreateLinkingConfirmationJobHandler(ILinkingConfirmationJobStore store)
{
    public async Task<CreateLinkingConfirmationJobOutcome> HandleAsync(
        int actorUserId,
        CreateLinkingConfirmationJobRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorUserId <= 0
            || request.PreflightId == Guid.Empty
            || request.IdempotencyKey == Guid.Empty
            || string.IsNullOrWhiteSpace(request.PreflightToken))
        {
            return new CreateLinkingConfirmationJobOutcome.InvalidRequest();
        }

        try
        {
            return new CreateLinkingConfirmationJobOutcome.Success(
                await store.EnqueueAsync(actorUserId, request, cancellationToken));
        }
        catch (LinkingConfirmationJobConflictException exception)
        {
            return new CreateLinkingConfirmationJobOutcome.Conflict(
                exception.Kind,
                exception.FailureCode);
        }
    }
}

public abstract record CreateLinkingConfirmationJobOutcome
{
    private CreateLinkingConfirmationJobOutcome() { }

    public sealed record Success(LinkingConfirmationJobReceipt Receipt)
        : CreateLinkingConfirmationJobOutcome;

    public sealed record InvalidRequest : CreateLinkingConfirmationJobOutcome;

    public sealed record Conflict(
        LinkingConfirmationJobConflictKind Kind,
        string FailureCode) : CreateLinkingConfirmationJobOutcome;
}
