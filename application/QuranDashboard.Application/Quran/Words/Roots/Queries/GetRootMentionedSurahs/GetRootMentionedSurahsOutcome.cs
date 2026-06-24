using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;

public abstract record GetRootMentionedSurahsOutcome
{
    private GetRootMentionedSurahsOutcome() { }

    public sealed record Success(RootSurahsResponse Surahs) : GetRootMentionedSurahsOutcome;
    public sealed record InvalidId : GetRootMentionedSurahsOutcome;
    public sealed record NotFound : GetRootMentionedSurahsOutcome;
}
