using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemAyahs;

public abstract record GetStemAyahsOutcome
{
    private GetStemAyahsOutcome() { }

    public sealed record Success(PagedResult<StemAyahMatchDto> Page) : GetStemAyahsOutcome;
    public sealed record InvalidId : GetStemAyahsOutcome;
    public sealed record InvalidPaging : GetStemAyahsOutcome;
    public sealed record NotFound : GetStemAyahsOutcome;
}
