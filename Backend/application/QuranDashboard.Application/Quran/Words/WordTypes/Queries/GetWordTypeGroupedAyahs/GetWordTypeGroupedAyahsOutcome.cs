using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;

public abstract record GetWordTypeGroupedAyahsOutcome
{
    private GetWordTypeGroupedAyahsOutcome() { }

    public sealed record Success(PagedResult<WordTypeAyahMatchDto> Page) : GetWordTypeGroupedAyahsOutcome;
    public sealed record InvalidKind : GetWordTypeGroupedAyahsOutcome;
    public sealed record InvalidId : GetWordTypeGroupedAyahsOutcome;
    public sealed record InvalidFilter : GetWordTypeGroupedAyahsOutcome;
    public sealed record InvalidPaging : GetWordTypeGroupedAyahsOutcome;
    public sealed record NotFound : GetWordTypeGroupedAyahsOutcome;
}
