using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.ListAccessUsers;

public sealed class ListAccessUsersHandler(IAccessUserReader reader)
{
    private const int MaximumSearchLength = 128;

    public Task<AccessOperationResult<PagedResult<AccessUserSummary>>> HandleAsync(
        AccessUserListQuery query,
        CancellationToken cancellationToken)
    {
        var search = NormalizeSearch(query.Search);
        if (!AccessAdministrationValidation.IsValidPage(query.Page, query.PageSize)
            || !IsValidStatus(query.Status)
            || !IsValidSearch(search))
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

    private static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static bool IsValidSearch(string? normalizedSearch) =>
        normalizedSearch is null || normalizedSearch.Length <= MaximumSearchLength;
}
