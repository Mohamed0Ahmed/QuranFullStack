using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;

public abstract record GetStemWordsOutcome
{
    private GetStemWordsOutcome() { }

    public sealed record Success(PagedResult<StemWordItemDto> Page) : GetStemWordsOutcome;
    public sealed record InvalidId : GetStemWordsOutcome;
    public sealed record InvalidKind : GetStemWordsOutcome;
    public sealed record InvalidPaging : GetStemWordsOutcome;
    public sealed record NotFound : GetStemWordsOutcome;
}
