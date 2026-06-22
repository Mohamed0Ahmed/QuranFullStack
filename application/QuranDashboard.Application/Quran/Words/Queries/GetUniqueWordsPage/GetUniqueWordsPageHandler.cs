using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

/// <summary>
/// Validates the Unique Words list request and delegates the bounded page read
/// to <see cref="IUniqueWordsReader"/>. Mirrors the established handler shape:
/// parse → validate bounds → read → map to a discriminated outcome. Expected
/// failures (bad kind/sort/paging) are outcome variants, never exceptions.
/// </summary>
public sealed class GetUniqueWordsPageHandler(IUniqueWordsReader reader)
{
    /// <summary>First page number (1-based).</summary>
    public const int MinPage = 1;

    /// <summary>Smallest accepted page size.</summary>
    public const int MinPageSize = 1;

    /// <summary>
    /// Largest accepted page size. Bounds unbounded client input; the list UI
    /// default is 50. Sized to stay responsive without over-fetching.
    /// </summary>
    public const int MaxPageSize = 200;

    private const string DefaultSortKey = "mushaf-order";

    public async Task<GetUniqueWordsPageOutcome> HandleAsync(
        GetUniqueWordsPageQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            return new GetUniqueWordsPageOutcome.InvalidKind();
        }

        // Null/empty sort is the documented default; an explicit unsupported
        // value is a controlled validation failure.
        var sortValue = string.IsNullOrWhiteSpace(query.Sort) ? DefaultSortKey : query.Sort;
        if (!UniqueWordSortParser.TryParse(sortValue, out var sort))
        {
            return new GetUniqueWordsPageOutcome.InvalidSort();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            return new GetUniqueWordsPageOutcome.InvalidPaging();
        }

        var page = await reader.GetUniqueWordsPageAsync(
            kind,
            query.Search,
            sort,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new GetUniqueWordsPageOutcome.Success(page);
    }
}
