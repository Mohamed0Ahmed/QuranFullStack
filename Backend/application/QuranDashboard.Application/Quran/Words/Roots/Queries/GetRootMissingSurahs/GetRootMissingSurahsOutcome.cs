using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;

public abstract record GetRootMissingSurahsOutcome
{
    private GetRootMissingSurahsOutcome() { }

    public sealed record Success(RootMissingSurahsResponse MissingSurahs) : GetRootMissingSurahsOutcome;
    public sealed record InvalidId : GetRootMissingSurahsOutcome;
    public sealed record NotFound : GetRootMissingSurahsOutcome;
}
