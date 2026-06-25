using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;

public abstract record GetLemmaMentionedSurahsOutcome
{
    private GetLemmaMentionedSurahsOutcome() { }

    public sealed record Success(LemmaSurahsResponse Surahs) : GetLemmaMentionedSurahsOutcome;
    public sealed record InvalidId : GetLemmaMentionedSurahsOutcome;
    public sealed record NotFound : GetLemmaMentionedSurahsOutcome;
}
