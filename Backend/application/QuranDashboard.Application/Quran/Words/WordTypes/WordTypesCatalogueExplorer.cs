using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public sealed class WordTypesCatalogueExplorer(
    ILogger<WordTypesCatalogueExplorer> logger,
    IWordTypesReader reader)
{
    public async Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        var tree = await reader.GetTreeAsync(cancellationToken);
        logger.LogInformation("Completed {feature} {operation} {itemCount}", "WordTypes", "GetWordTypeTree", tree.MainTypes.Count);
        return tree;
    }

    public async Task<WordTypesCatalogueResult.Rows> GetRowsAsync(
        string? type, string? childCode, string? @case, string? tense, string? voice,
        string? search, string? sort, int page, int pageSize,
        bool? hasRoot, bool? hasStem, bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var filter = WordTypeFilter.Create(type, childCode, @case, tense, voice, search, hasRoot, hasStem, hasLemma);
        if (filter is null)
        {
            LogInvalidFilter("GetWordTypeRows", type, childCode, @case, tense, voice, search);
            return new WordTypesCatalogueResult.Rows.InvalidFilter();
        }

        var sortSpec = WordTypeSortSpec.Create(sort);
        if (sortSpec is null)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {type} {childCode}", "WordTypes", "GetWordTypeRows", "invalidSort", filter.Scope.Type, filter.Scope.ChildCode);
            return new WordTypesCatalogueResult.Rows.InvalidSort();
        }

        var paging = WordTypeListPaging.Create(page, pageSize);
        if (paging is null)
        {
            return new WordTypesCatalogueResult.Rows.InvalidPaging();
        }

        var result = await reader.GetRowsAsync(filter, sortSpec, paging, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {pageNumber} {pageSize} {sort} {totalCount} {itemCount}",
            "WordTypes", "GetWordTypeRows", filter.Scope.Type, filter.Scope.ChildCode, filter.Search is not null,
            paging.Page, paging.PageSize, sortSpec.CanonicalToken(), result.TotalCount, result.Items.Count);
        return new WordTypesCatalogueResult.Rows.Success(result);
    }

    public async Task<WordTypesCatalogueResult.Table> GetTableAsync(
        string? tableView, string? type, string? childCode, string? @case, string? tense, string? voice,
        string? search, string? sort, int page, int pageSize,
        bool? hasRoot, bool? hasStem, bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var filter = WordTypeFilter.Create(type, childCode, @case, tense, voice, search, hasRoot, hasStem, hasLemma);
        if (filter is null)
        {
            LogInvalidFilter("GetWordTypeTable", type, childCode, @case, tense, voice, search);
            return new WordTypesCatalogueResult.Table.InvalidFilter();
        }

        var view = WordTypeTableView.Create(tableView);
        if (view is null)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", "WordTypes", "GetWordTypeTable", "invalidTableView");
            return new WordTypesCatalogueResult.Table.InvalidTableView();
        }

        var sortSpec = WordTypeSortSpec.Create(sort);
        if (sortSpec is null)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", "WordTypes", "GetWordTypeTable", "invalidSort");
            return new WordTypesCatalogueResult.Table.InvalidSort();
        }

        var paging = WordTypeListPaging.Create(page, pageSize);
        if (paging is null)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {pageNumber} {pageSize}", "WordTypes", "GetWordTypeTable", "invalidPaging", page, pageSize);
            return new WordTypesCatalogueResult.Table.InvalidPaging();
        }

        var result = await reader.GetTableRowsAsync(filter, view, sortSpec, paging, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {tableView} {pageNumber} {pageSize} {sort} {totalCount} {itemCount}",
            "WordTypes", "GetWordTypeTable", filter.Scope.Type, filter.Scope.ChildCode, filter.Search is not null,
            view.Key, paging.Page, paging.PageSize, sortSpec.CanonicalToken(), result.TotalCount, result.Items.Count);
        return new WordTypesCatalogueResult.Table.Success(result);
    }

    public async Task<WordTypesCatalogueResult.ScopeCounts> GetScopeCountsAsync(
        string? type, string? childCode, string? @case, string? tense, string? voice,
        string? search, bool? hasRoot, bool? hasStem, bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var filter = WordTypeFilter.Create(type, childCode, @case, tense, voice, search, hasRoot, hasStem, hasLemma);
        if (filter is null)
        {
            LogInvalidFilter("GetWordTypeScopeCounts", type, childCode, @case, tense, voice, search);
            return new WordTypesCatalogueResult.ScopeCounts.InvalidFilter();
        }

        var counts = await reader.GetScopeCountsAsync(filter, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {wordsCount} {rootsCount} {stemsCount} {lemmasCount}",
            "WordTypes", "GetWordTypeScopeCounts", filter.Scope.Type, filter.Scope.ChildCode, filter.Search is not null,
            counts.WordsCount, counts.RootsCount, counts.StemsCount, counts.LemmasCount);
        return new WordTypesCatalogueResult.ScopeCounts.Success(counts);
    }

    private void LogInvalidFilter(
        string operation, string? type, string? childCode, string? @case, string? tense, string? voice, string? search) =>
        logger.LogWarning(
            "Rejected {feature} {operation} {reason} {hasType} {hasChildCode} {hasCaseFilter} {hasTenseFilter} {hasVoiceFilter} {hasSearch}",
            "WordTypes", operation, "invalidFilter",
            !string.IsNullOrWhiteSpace(type),
            !string.IsNullOrWhiteSpace(childCode),
            !string.IsNullOrWhiteSpace(@case), !string.IsNullOrWhiteSpace(tense), !string.IsNullOrWhiteSpace(voice), !string.IsNullOrWhiteSpace(search));
}
