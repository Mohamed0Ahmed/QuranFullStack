using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class GetLinkingPreparedDetailPageHandler(
    ILinkingPreparedPreflightStore store,
    ILinkingScalabilityPolicy policy)
{
    public async Task<GetLinkingPreparedDetailPageOutcome> HandleAsync(
        int actorUserId,
        Guid preflightId,
        long? preparedSourceId,
        string filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page <= 0
            || pageSize <= 0
            || pageSize > policy.PageSizeMaximum
            || !LinkingPreparedDetailFilters.All.Contains(filter)
            || preparedSourceId is <= 0)
        {
            return new GetLinkingPreparedDetailPageOutcome.InvalidRequest();
        }

        try
        {
            var result = await store.GetDetailPageAsync(
                actorUserId,
                preflightId,
                preparedSourceId,
                filter,
                page,
                pageSize,
                cancellationToken);
            return result is null
                ? new GetLinkingPreparedDetailPageOutcome.NotFound()
                : new GetLinkingPreparedDetailPageOutcome.Success(result);
        }
        catch (LinkingPreparedPreflightLifecycleException exception)
        {
            return new GetLinkingPreparedDetailPageOutcome.Conflict(
                LinkingPreparedPreflightLifecycleTokens.ToToken(exception.FailureCode)!,
                exception.Expired);
        }
        catch (LinkingDataStaleException)
        {
            return new GetLinkingPreparedDetailPageOutcome.Conflict("LINKING_DATA_STALE", false);
        }
        catch (LinkingPageOutOfRangeException)
        {
            return new GetLinkingPreparedDetailPageOutcome.InvalidRequest();
        }
    }
}

public abstract record GetLinkingPreparedDetailPageOutcome
{
    private GetLinkingPreparedDetailPageOutcome() { }

    public sealed record Success(LinkingPreparedDetailPageDto Page)
        : GetLinkingPreparedDetailPageOutcome;
    public sealed record InvalidRequest : GetLinkingPreparedDetailPageOutcome;
    public sealed record NotFound : GetLinkingPreparedDetailPageOutcome;
    public sealed record Conflict(string FailureCode, bool Expired)
        : GetLinkingPreparedDetailPageOutcome;
}
