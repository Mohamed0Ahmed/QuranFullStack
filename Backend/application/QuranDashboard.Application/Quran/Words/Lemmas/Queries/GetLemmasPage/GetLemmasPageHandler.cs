using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;

public sealed class GetLemmasPageHandler(
    ILogger<GetLemmasPageHandler> logger,
    ILemmasReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmasPage";
    private const string DefaultSortKey = LemmaSortKeys.MushafOrder;

    public async Task<GetLemmasPageOutcome> HandleAsync(
        GetLemmasPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? DefaultSortKey : query.Sort;
        if (!LemmaSortParser.TryParse(sortValue, out var sort))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidSort",
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetLemmasPageOutcome.InvalidSort();
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
                sort.CanonicalToken(),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetLemmasPageOutcome.InvalidPaging();
        }

        var filter = query.Filter ?? LemmasCountFilter.None;
        var association = query.Association ?? LemmasAssociationFilter.None;
        if (!filter.IsValid || !association.IsValid)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {sort} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidFilter",
                sort.CanonicalToken(),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetLemmasPageOutcome.InvalidFilter();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetLemmasPageAsync(
            query.Search,
            sort,
            filter,
            association,
            query.Page,
            query.PageSize,
            cancellationToken);

        logger.LogInformation(
            "Completed {feature} {operation} {sort} {pageNumber} {pageSize} {totalCount} {itemCount} {hasSearch}",
            FeatureName,
            OperationName,
            sort.CanonicalToken(),
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            hasSearch);

        return new GetLemmasPageOutcome.Success(page);
    }

    private static bool HasSearch(string? search) => !string.IsNullOrWhiteSpace(search);
}
