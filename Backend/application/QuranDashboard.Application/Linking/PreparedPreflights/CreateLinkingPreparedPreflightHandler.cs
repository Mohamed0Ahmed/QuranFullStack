using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class CreateLinkingPreparedPreflightHandler(ILinkingPreparedPreflightStore store)
{
    public async Task<CreateLinkingPreparedPreflightOutcome> HandleAsync(
        int actorUserId,
        CreateLinkingPreparedPreflightRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PreparationKey == Guid.Empty
            || request.DoorId <= 0
            || request.ExpectedLinkingDataRevision is <= 0
            || request.Sources.Count is < 1 or > LinkingLimits.MaxPreparedSources
            || !request.Sources.Select(source => source.OrderValue).Order()
                .SequenceEqual(Enumerable.Range(1, request.Sources.Count))
            || request.Sources.Any(source =>
                (source.WorkspaceSource is null) == (source.InlineSource is null)))
        {
            return new CreateLinkingPreparedPreflightOutcome.InvalidRequest();
        }

        try
        {
            var receipt = await store.EnqueueAsync(actorUserId, request, cancellationToken);
            return new CreateLinkingPreparedPreflightOutcome.Success(
                receipt,
                receipt.IsNew || !IsTerminal(receipt.Status.Status));
        }
        catch (LinkingPreparedPreflightLifecycleException exception)
        {
            return new CreateLinkingPreparedPreflightOutcome.Conflict(
                LinkingPreparedPreflightLifecycleTokens.ToToken(exception.FailureCode)!);
        }
        catch (LinkingDataStaleException)
        {
            return new CreateLinkingPreparedPreflightOutcome.Conflict("LINKING_DATA_STALE");
        }
        catch (LinkingSourceNotFoundException)
        {
            return new CreateLinkingPreparedPreflightOutcome.NotFound();
        }
    }

    private static bool IsTerminal(string status) => status is
        "stale" or "failed" or "cancelled" or "expired" or "confirmed";
}

public abstract record CreateLinkingPreparedPreflightOutcome
{
    private CreateLinkingPreparedPreflightOutcome() { }

    public sealed record Success(LinkingPreparedPreflightReceipt Receipt, bool Accepted)
        : CreateLinkingPreparedPreflightOutcome;
    public sealed record InvalidRequest : CreateLinkingPreparedPreflightOutcome;
    public sealed record NotFound : CreateLinkingPreparedPreflightOutcome;
    public sealed record Conflict(string FailureCode) : CreateLinkingPreparedPreflightOutcome;
}
