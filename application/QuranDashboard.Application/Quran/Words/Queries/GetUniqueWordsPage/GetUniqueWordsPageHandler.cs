using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public sealed class GetUniqueWordsPageHandler(IUniqueWordsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    /// <remarks>
    /// Bounds unbounded client input; the list UI default is 50.
    /// </remarks>
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
