using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Common.Paging;

namespace QuranDashboard.Application.Access.Queries.ListAccessUsers;

public sealed class ListAccessUsersHandler(
    IAccessUserReader reader,
    IEmailIdentityNormalizer emailIdentityNormalizer)
{
    public Task<AccessOperationResult<PagedResult<AccessUserSummary>>> HandleAsync(
        AccessUserListQuery query,
        CancellationToken cancellationToken)
    {
        if (!AccessAdministrationValidation.IsValidPage(query.Page, query.PageSize)
            || !IsValidStatus(query.Status)
            || !TryNormalizeSearch(query.Search, out var search))
        {
            return Task.FromResult(AccessOperationResult<PagedResult<AccessUserSummary>>.Failed(
                AccessOperationFailure.InvalidRequest));
        }

        return ReadAsync(query with { Search = search }, cancellationToken);
    }

    private async Task<AccessOperationResult<PagedResult<AccessUserSummary>>> ReadAsync(
        AccessUserListQuery query,
        CancellationToken cancellationToken)
    {
        var page = await reader.ListAsync(query, cancellationToken);
        return AccessOperationResult<PagedResult<AccessUserSummary>>.Success(page);
    }

    private static bool IsValidStatus(string? status) => status is null
        || string.Equals(status, "pending", StringComparison.Ordinal)
        || string.Equals(status, "active", StringComparison.Ordinal)
        || string.Equals(status, "disabled", StringComparison.Ordinal);

    private bool TryNormalizeSearch(string? search, out string? normalizedSearch)
    {
        normalizedSearch = null;
        if (string.IsNullOrWhiteSpace(search))
        {
            return search is null;
        }

        return emailIdentityNormalizer.TryNormalize(search, out normalizedSearch);
    }
}
