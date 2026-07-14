using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

public abstract record GetUniqueWordsPageOutcome
{
    private GetUniqueWordsPageOutcome() { }

    public sealed record Success(PagedResult<UniqueWordListItemDto> Page) : GetUniqueWordsPageOutcome;
    public sealed record InvalidKind : GetUniqueWordsPageOutcome;
    public sealed record InvalidSort : GetUniqueWordsPageOutcome;
    public sealed record InvalidPaging : GetUniqueWordsPageOutcome;
    public sealed record InvalidFilter : GetUniqueWordsPageOutcome;
}
