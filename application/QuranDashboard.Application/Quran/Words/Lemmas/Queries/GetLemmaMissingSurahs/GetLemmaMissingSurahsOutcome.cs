using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;

public abstract record GetLemmaMissingSurahsOutcome
{
    private GetLemmaMissingSurahsOutcome() { }

    public sealed record Success(LemmaMissingSurahsResponse MissingSurahs) : GetLemmaMissingSurahsOutcome;
    public sealed record InvalidId : GetLemmaMissingSurahsOutcome;
    public sealed record NotFound : GetLemmaMissingSurahsOutcome;
}
