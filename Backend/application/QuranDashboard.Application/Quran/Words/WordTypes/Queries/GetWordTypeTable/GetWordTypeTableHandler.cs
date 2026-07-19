using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;

public sealed class GetWordTypeTableHandler(
    ILogger<GetWordTypeTableHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeTable";

    public async Task<GetWordTypeTableOutcome> HandleAsync(
        GetWordTypeTableQuery query,
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

            return new GetWordTypeTableOutcome.InvalidFilter();
        }

        if (!WordTypeTableViewParser.TryParse(query.TableView, out var tableView))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {tableView}",
                FeatureName,
                OperationName,
                "invalidTableView",
                query.TableView);

            return new GetWordTypeTableOutcome.InvalidTableView();
        }

        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? WordTypesHandlerValidation.DefaultSort : query.Sort;
        if (!WordTypeSortParser.TryParse(sortValue, out var sort))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {sort}",
                FeatureName,
                OperationName,
                "invalidSort",
                sortValue);

            return new GetWordTypeTableOutcome.InvalidSort();
        }

        if (!WordTypesHandlerValidation.IsValidListPaging(query.Page, query.PageSize))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                query.Page,
                query.PageSize);

            return new GetWordTypeTableOutcome.InvalidPaging();
        }

        var page = await reader.GetTableRowsAsync(filter, tableView, sort, query.Page, query.PageSize, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {tableView} {pageNumber} {pageSize} {sort} {totalCount} {itemCount}",
            FeatureName,
            OperationName,
            filter.Type,
            filter.ChildCode,
            filter.Search is not null,
            query.TableView,
            query.Page,
            query.PageSize,
            sort.CanonicalToken(),
            page.TotalCount,
            page.Items.Count);

        return new GetWordTypeTableOutcome.Success(page);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
