using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

public abstract record GetRootWordsOutcome
{
    private GetRootWordsOutcome() { }

    public sealed record Success(PagedResult<RootWordItemDto> Page) : GetRootWordsOutcome;
    public sealed record InvalidId : GetRootWordsOutcome;
    public sealed record InvalidKind : GetRootWordsOutcome;
    public sealed record InvalidPaging : GetRootWordsOutcome;
    public sealed record NotFound : GetRootWordsOutcome;
}
