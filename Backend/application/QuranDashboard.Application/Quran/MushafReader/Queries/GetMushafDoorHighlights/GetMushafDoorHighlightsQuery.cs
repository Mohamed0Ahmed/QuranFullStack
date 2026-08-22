namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafDoorHighlights;

public sealed record GetMushafDoorHighlightsQuery(
    int PageNumber,
    IReadOnlyList<int> DoorIds);
