using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

/// <summary>
/// Discriminated handler outcome so the controller maps to 200 / 400 / 404
/// without exceptions for expected cases.
/// </summary>
public abstract record GetAyahStudyOutcome
{
    private GetAyahStudyOutcome() { }

    public sealed record Success(AyahStudyResponse Response) : GetAyahStudyOutcome;

    public sealed record InvalidVerseKey : GetAyahStudyOutcome;

    public sealed record NotFound : GetAyahStudyOutcome;
}
