using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;

public sealed class GetUniqueWordAyahsHandler(IUniqueWordsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    public async Task<GetUniqueWordAyahsOutcome> HandleAsync(
        GetUniqueWordAyahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            return new GetUniqueWordAyahsOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            return new GetUniqueWordAyahsOutcome.InvalidId();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            return new GetUniqueWordAyahsOutcome.InvalidPaging();
        }

        var page = await reader.GetAyahMatchesAsync(
            kind,
            query.Id,
            query.Page,
            query.PageSize,
            cancellationToken);

        if (page is null)
        {
            return new GetUniqueWordAyahsOutcome.NotFound();
        }

        return new GetUniqueWordAyahsOutcome.Success(page);
    }
}
