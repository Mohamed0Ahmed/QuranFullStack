using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafDoorHighlights;

public abstract record GetMushafDoorHighlightsOutcome
{
    private GetMushafDoorHighlightsOutcome() { }

    public sealed record Success(MushafDoorHighlightsResponse Response) : GetMushafDoorHighlightsOutcome;

    public sealed record InvalidPageNumber : GetMushafDoorHighlightsOutcome;

    public sealed record InvalidDoorIds : GetMushafDoorHighlightsOutcome;
}
