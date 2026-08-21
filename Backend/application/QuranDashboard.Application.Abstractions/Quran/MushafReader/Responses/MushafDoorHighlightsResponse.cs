namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record MushafDoorHighlightsResponse(
    int PageNumber,
    IReadOnlyList<MushafDoorAyahHighlightDto> Ayahs,
    IReadOnlyList<MushafDoorWordHighlightDto> Words,
    IReadOnlyList<int> UnavailableDoorIds);

public sealed record MushafDoorAyahHighlightDto(
    string VerseKey,
    IReadOnlyList<int> DoorIds);

public sealed record MushafDoorWordHighlightDto(
    string WordLocation,
    IReadOnlyList<int> DoorIds);
