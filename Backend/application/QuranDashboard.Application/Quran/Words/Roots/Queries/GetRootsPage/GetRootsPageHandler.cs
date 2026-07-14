using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

public sealed class GetRootsPageHandler(
    ILogger<GetRootsPageHandler> logger,
    IRootsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    public const int MaxPageSize = 1000;

    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootsPage";
    private const string DefaultSortKey = RootSortKeys.MushafOrder;

    public async Task<GetRootsPageOutcome> HandleAsync(
        GetRootsPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

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

        var filter = query.Filter ?? RootsCountFilter.None;
        if (!filter.IsValid)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {sort} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidFilter",
                GetSortKey(sort),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetRootsPageOutcome.InvalidFilter();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetRootsPageAsync(
            query.Search,
            sort,
            filter,
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
