using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

/// <summary>
/// Validates and serves the Roots list page (US1, T025). Validates sort + paging,
/// delegates the read to <see cref="IRootsReader"/>, and emits a structured log
/// carrying IDs/counts/<c>hasSearch</c> only — never root/search text. Mirrors
/// <c>GetUniqueWordsPageHandler</c>.
/// </summary>
public sealed class GetRootsPageHandler(
    ILogger<GetRootsPageHandler> logger,
    IRootsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    /// <remarks>Bounds unbounded client input; the list UI default is 1000.</remarks>
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootsPage";
    private const string DefaultSortKey = RootSortKeys.MushafOrder;

    public async Task<GetRootsPageOutcome> HandleAsync(
        GetRootsPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Null/empty sort is the documented default; an explicit unsupported
        // value is a controlled validation failure.
        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? DefaultSortKey : query.Sort;
        if (!RootSortParser.TryParse(sortValue, out var sort))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidSort",
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetRootsPageOutcome.InvalidSort();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {sort} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidPaging",
                GetSortKey(sort),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetRootsPageOutcome.InvalidPaging();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetRootsPageAsync(
            query.Search,
            sort,
            query.Page,
            query.PageSize,
            cancellationToken);

        logger.LogInformation(
            "Completed {feature} {operation} {sort} {pageNumber} {pageSize} {totalCount} {itemCount} {hasSearch}",
            FeatureName,
            OperationName,
            GetSortKey(sort),
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            hasSearch);

        return new GetRootsPageOutcome.Success(page);
    }

    private static string GetSortKey(RootSort sort) => sort switch
    {
        RootSort.MushafOrder => RootSortKeys.MushafOrder,
        RootSort.Occurrences => RootSortKeys.Occurrences,
        RootSort.Alpha => RootSortKeys.Alpha,
        _ => throw new InvalidOperationException($"Unhandled {nameof(RootSort)} value."),
    };

    private static bool HasSearch(string? search) => !string.IsNullOrWhiteSpace(search);
}
