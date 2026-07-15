using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

public abstract record GetRootsPageOutcome
{
    private GetRootsPageOutcome() { }

    public sealed record Success(PagedResult<RootListItemDto> Page) : GetRootsPageOutcome;
    public sealed record InvalidSort : GetRootsPageOutcome;
    public sealed record InvalidPaging : GetRootsPageOutcome;
    public sealed record InvalidFilter : GetRootsPageOutcome;
}
