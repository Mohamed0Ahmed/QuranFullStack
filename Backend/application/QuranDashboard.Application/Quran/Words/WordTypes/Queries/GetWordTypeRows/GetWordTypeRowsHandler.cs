using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

public sealed class GetWordTypeRowsHandler(
    ILogger<GetWordTypeRowsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeRows";

    public async Task<GetWordTypeRowsOutcome> HandleAsync(
        GetWordTypeRowsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var search = WordTypesHandlerValidation.NormalizeSearch(query.Search);
        var filter = new WordTypeFilter(
            WordTypesHandlerValidation.NormalizeType(query.Type),
            Normalize(query.ChildCode),
            Normalize(query.Case),
            Normalize(query.Tense),
            Normalize(query.Voice),
            search,
            query.HasRoot,
            query.HasStem,
            query.HasLemma);

        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {type} {childCode} {hasCaseFilter} {hasTenseFilter} {hasVoiceFilter} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidFilter",
                filter.Type,
                filter.ChildCode,
                filter.Case is not null,
                filter.Tense is not null,
                filter.Voice is not null,
                filter.Search is not null);

            return new GetWordTypeRowsOutcome.InvalidFilter();
        }

        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? WordTypesHandlerValidation.DefaultSort : query.Sort;
        if (!WordTypeSortParser.TryParse(sortValue, out var sort))
        {
            return new GetWordTypeRowsOutcome.InvalidSort();
        }

        if (!WordTypesHandlerValidation.IsValidListPaging(query.Page, query.PageSize))
        {
            return new GetWordTypeRowsOutcome.InvalidPaging();
        }

        var page = await reader.GetRowsAsync(filter, sort, query.Page, query.PageSize, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {pageNumber} {pageSize} {sort} {totalCount} {itemCount}",
            FeatureName,
            OperationName,
            filter.Type,
            filter.ChildCode,
            filter.Search is not null,
            query.Page,
            query.PageSize,
            sortValue,
            page.TotalCount,
            page.Items.Count);

        return new GetWordTypeRowsOutcome.Success(page);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
