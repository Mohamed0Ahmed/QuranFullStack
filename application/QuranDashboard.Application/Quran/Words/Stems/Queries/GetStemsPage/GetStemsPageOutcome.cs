using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

public abstract record GetStemsPageOutcome
{
    private GetStemsPageOutcome() { }

    public sealed record Success(PagedResult<StemListItemDto> Page) : GetStemsPageOutcome;
    public sealed record InvalidSort : GetStemsPageOutcome;
    public sealed record InvalidPaging : GetStemsPageOutcome;
}
