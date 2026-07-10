using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

public abstract record GetAyahStudyOutcome
{
    private GetAyahStudyOutcome() { }

    public sealed record Success(AyahStudyResponse Response) : GetAyahStudyOutcome;

    public sealed record InvalidVerseKey : GetAyahStudyOutcome;

    public sealed record NotFound : GetAyahStudyOutcome;
}
