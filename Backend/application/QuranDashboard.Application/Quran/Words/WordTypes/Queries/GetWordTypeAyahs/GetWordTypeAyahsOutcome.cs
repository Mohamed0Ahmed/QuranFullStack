using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;

public abstract record GetWordTypeAyahsOutcome
{
    private GetWordTypeAyahsOutcome() { }

    public sealed record Success(PagedResult<WordTypeAyahMatchDto> Page) : GetWordTypeAyahsOutcome;
    public sealed record InvalidIdentity : GetWordTypeAyahsOutcome;
    public sealed record InvalidPaging : GetWordTypeAyahsOutcome;
    public sealed record NotFound : GetWordTypeAyahsOutcome;
}
