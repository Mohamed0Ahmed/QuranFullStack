using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;

public abstract record GetWordTypeGroupedWordsOutcome
{
    private GetWordTypeGroupedWordsOutcome() { }

    public sealed record Success(PagedResult<WordTypeGroupedMemberWordDto> Page) : GetWordTypeGroupedWordsOutcome;
    public sealed record InvalidKind : GetWordTypeGroupedWordsOutcome;
    public sealed record InvalidId : GetWordTypeGroupedWordsOutcome;
    public sealed record InvalidFilter : GetWordTypeGroupedWordsOutcome;
    public sealed record InvalidPaging : GetWordTypeGroupedWordsOutcome;
    public sealed record NotFound : GetWordTypeGroupedWordsOutcome;
}
