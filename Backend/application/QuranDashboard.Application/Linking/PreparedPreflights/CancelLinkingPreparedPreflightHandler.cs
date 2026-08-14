using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class CancelLinkingPreparedPreflightHandler(ILinkingPreparedPreflightStore store)
{
    public async Task<CancelLinkingPreparedPreflightOutcome> HandleAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await store.CancelAsync(actorUserId, preflightId, cancellationToken);
            return status is null
                ? new CancelLinkingPreparedPreflightOutcome.NotFound()
                : new CancelLinkingPreparedPreflightOutcome.Success(status);
        }
        catch (LinkingPreparedPreflightLifecycleException exception)
        {
            return new CancelLinkingPreparedPreflightOutcome.Conflict(
                LinkingPreparedPreflightLifecycleTokens.ToToken(exception.FailureCode)!,
                exception.Expired);
        }
    }
}

public abstract record CancelLinkingPreparedPreflightOutcome
{
    private CancelLinkingPreparedPreflightOutcome() { }

    public sealed record Success(LinkingPreparedPreflightStatusDto Status)
        : CancelLinkingPreparedPreflightOutcome;
    public sealed record NotFound : CancelLinkingPreparedPreflightOutcome;
    public sealed record Conflict(string FailureCode, bool Expired)
        : CancelLinkingPreparedPreflightOutcome;
}
