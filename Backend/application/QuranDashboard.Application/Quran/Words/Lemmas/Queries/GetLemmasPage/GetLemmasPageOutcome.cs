using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;

public abstract record GetLemmasPageOutcome
{
    private GetLemmasPageOutcome() { }

    public sealed record Success(PagedResult<LemmaListItemDto> Page) : GetLemmasPageOutcome;
    public sealed record InvalidSort : GetLemmasPageOutcome;
    public sealed record InvalidPaging : GetLemmasPageOutcome;
    public sealed record InvalidFilter : GetLemmasPageOutcome;
}
