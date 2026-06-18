using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

/// <summary>
/// Discriminated handler outcome so the controller maps to 200 / 400 / 404
/// without exceptions for expected cases.
/// </summary>
public abstract record GetMushafPageOutcome
{
    private GetMushafPageOutcome() { }

    public sealed record Success(MushafPageResponse Response) : GetMushafPageOutcome;

    public sealed record InvalidPageNumber : GetMushafPageOutcome;

    public sealed record NotFound : GetMushafPageOutcome;
}
