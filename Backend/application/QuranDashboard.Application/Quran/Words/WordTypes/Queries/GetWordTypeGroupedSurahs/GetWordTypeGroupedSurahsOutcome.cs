using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

public abstract record GetWordTypeGroupedSurahsOutcome
{
    private GetWordTypeGroupedSurahsOutcome() { }

    public sealed record Success(WordTypeSurahsResponse Surahs) : GetWordTypeGroupedSurahsOutcome;
    public sealed record InvalidKind : GetWordTypeGroupedSurahsOutcome;
    public sealed record InvalidId : GetWordTypeGroupedSurahsOutcome;
    public sealed record InvalidFilter : GetWordTypeGroupedSurahsOutcome;
    public sealed record NotFound : GetWordTypeGroupedSurahsOutcome;
}
