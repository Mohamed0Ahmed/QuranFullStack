using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

public sealed class GetStemsPageHandler(
    ILogger<GetStemsPageHandler> logger,
    IStemsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemsPage";
    private const string DefaultSortKey = StemSortKeys.MushafOrder;

    public async Task<GetStemsPageOutcome> HandleAsync(
        GetStemsPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? DefaultSortKey : query.Sort;
        if (!StemSortParser.TryParse(sortValue, out var sort))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidSort",
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetStemsPageOutcome.InvalidSort();
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

            return new GetStemsPageOutcome.InvalidPaging();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetStemsPageAsync(
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

        return new GetStemsPageOutcome.Success(page);
    }

    private static string GetSortKey(StemSort sort) => sort switch
    {
        StemSort.MushafOrder => StemSortKeys.MushafOrder,
        StemSort.Occurrences => StemSortKeys.Occurrences,
        StemSort.Alpha => StemSortKeys.Alpha,
        _ => throw new InvalidOperationException($"Unhandled {nameof(StemSort)} value."),
    };

    private static bool HasSearch(string? search) => !string.IsNullOrWhiteSpace(search);
}
