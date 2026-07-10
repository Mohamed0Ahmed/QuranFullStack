using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public sealed class GetUniqueWordsPageHandler(
    ILogger<GetUniqueWordsPageHandler> logger,
    IUniqueWordsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    public const int MaxPageSize = 1000;

    private const string FeatureName = "Words";
    private const string OperationName = "GetUniqueWordsPage";
    private const string DefaultSortKey = UniqueWordSortKeys.MushafOrder;

    public async Task<GetUniqueWordsPageOutcome> HandleAsync(
        GetUniqueWordsPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidKind",
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetUniqueWordsPageOutcome.InvalidKind();
        }

        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? DefaultSortKey : query.Sort;
        if (!UniqueWordSortParser.TryParse(sortValue, out var sort))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidSort",
                GetKindKey(kind),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetUniqueWordsPageOutcome.InvalidSort();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {sort} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidPaging",
                GetKindKey(kind),
                GetSortKey(sort),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetUniqueWordsPageOutcome.InvalidPaging();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetUniqueWordsPageAsync(
            kind,
            query.Search,
            sort,
            query.Page,
            query.PageSize,
            cancellationToken);

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {sort} {pageNumber} {pageSize} {totalCount} {itemCount} {hasSearch}",
            FeatureName,
            OperationName,
            GetKindKey(kind),
            GetSortKey(sort),
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            hasSearch);

        return new GetUniqueWordsPageOutcome.Success(page);
    }

    private static string GetKindKey(UniqueWordKind kind) =>
        kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;

    private static string GetSortKey(UniqueWordSort sort) =>
        sort switch
        {
            UniqueWordSort.MushafOrder => UniqueWordSortKeys.MushafOrder,
            UniqueWordSort.Occurrences => UniqueWordSortKeys.Occurrences,
            UniqueWordSort.Alpha => UniqueWordSortKeys.Alpha,
            _ => throw new InvalidOperationException($"Unhandled {nameof(UniqueWordSort)} value."),
        };

    private static bool HasSearch(string? search) => !string.IsNullOrWhiteSpace(search);
}
