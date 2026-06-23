using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;

public abstract record GetUniqueWordSurahsOutcome
{
    private GetUniqueWordSurahsOutcome() { }

    public sealed record Success(UniqueWordSurahsResponse Response) : GetUniqueWordSurahsOutcome;
    public sealed record InvalidKind : GetUniqueWordSurahsOutcome;
    public sealed record InvalidId : GetUniqueWordSurahsOutcome;
    public sealed record NotFound : GetUniqueWordSurahsOutcome;
}
