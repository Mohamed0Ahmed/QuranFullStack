using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

public abstract record GetWordTypeRowsOutcome
{
    private GetWordTypeRowsOutcome() { }

    public sealed record Success(PagedResult<WordTypeRowDto> Page) : GetWordTypeRowsOutcome;
    public sealed record InvalidFilter : GetWordTypeRowsOutcome;
    public sealed record InvalidSort : GetWordTypeRowsOutcome;
    public sealed record InvalidPaging : GetWordTypeRowsOutcome;
}
