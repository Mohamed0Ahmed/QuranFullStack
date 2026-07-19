using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public sealed class GetUniqueWordsPageHandler(
    ILogger<GetUniqueWordsPageHandler> logger,
    IUniqueWordsReader reader,
    IPosTagCatalogueReader posCatalogue)
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
                sort.CanonicalToken(),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetUniqueWordsPageOutcome.InvalidPaging();
        }

        var filter = query.Filter ?? UniqueWordsCountFilter.None;
        var association = query.Association ?? UniqueWordsAssociationFilter.None;
        if (!filter.IsValid
            || !association.IsValid
            || !await IsAssociationTypeKnownAsync(association, cancellationToken))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {sort} {pageNumber} {pageSize} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidFilter",
                GetKindKey(kind),
                sort.CanonicalToken(),
                query.Page,
                query.PageSize,
                HasSearch(query.Search));

            return new GetUniqueWordsPageOutcome.InvalidFilter();
        }

        var hasSearch = HasSearch(query.Search);
        var page = await reader.GetUniqueWordsPageAsync(
            kind,
            query.Search,
            sort,
            filter,
            association,
            query.Page,
            query.PageSize,
            cancellationToken);

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {sort} {pageNumber} {pageSize} {totalCount} {itemCount} {hasSearch}",
            FeatureName,
            OperationName,
            GetKindKey(kind),
            sort.CanonicalToken(),
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

    private static bool HasSearch(string? search) => !string.IsNullOrWhiteSpace(search);

    // The primary-type filter value must exist in the POS catalogue; an unknown code is a controlled
    // 400 (InvalidFilter), not a silent empty result. Absent/empty primaryType needs no lookup.
    private async Task<bool> IsAssociationTypeKnownAsync(
        UniqueWordsAssociationFilter association,
        CancellationToken cancellationToken)
    {
        if (association.NormalizedPrimaryType is not { } code)
        {
            return true;
        }

        return await posCatalogue.ExistsAsync(code, cancellationToken);
    }
}
