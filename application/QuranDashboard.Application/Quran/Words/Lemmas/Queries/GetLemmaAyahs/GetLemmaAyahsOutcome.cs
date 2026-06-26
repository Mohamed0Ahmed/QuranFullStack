using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;

public abstract record GetLemmaAyahsOutcome
{
    private GetLemmaAyahsOutcome() { }

    public sealed record Success(PagedResult<LemmaAyahMatchDto> Page) : GetLemmaAyahsOutcome;
    public sealed record InvalidId : GetLemmaAyahsOutcome;
    public sealed record InvalidPaging : GetLemmaAyahsOutcome;
    public sealed record NotFound : GetLemmaAyahsOutcome;
}
