using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;

public abstract record GetWordTypeSurahsOutcome
{
    private GetWordTypeSurahsOutcome() { }

    public sealed record Success(WordTypeSurahsResponse Surahs) : GetWordTypeSurahsOutcome;
    public sealed record InvalidIdentity : GetWordTypeSurahsOutcome;
    public sealed record NotFound : GetWordTypeSurahsOutcome;
}
