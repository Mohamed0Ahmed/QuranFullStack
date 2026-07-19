using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;

public abstract record GetLemmaWordsOutcome
{
    private GetLemmaWordsOutcome() { }

    public sealed record Success(PagedResult<LemmaWordItemDto> Page) : GetLemmaWordsOutcome;
    public sealed record InvalidId : GetLemmaWordsOutcome;
    public sealed record InvalidKind : GetLemmaWordsOutcome;
    public sealed record InvalidPaging : GetLemmaWordsOutcome;
    public sealed record NotFound : GetLemmaWordsOutcome;
}
