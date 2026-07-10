using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMissingSurahs;

public abstract record GetStemMissingSurahsOutcome
{
    private GetStemMissingSurahsOutcome() { }

    public sealed record Success(StemMissingSurahsResponse MissingSurahs) : GetStemMissingSurahsOutcome;
    public sealed record InvalidId : GetStemMissingSurahsOutcome;
    public sealed record NotFound : GetStemMissingSurahsOutcome;
}
