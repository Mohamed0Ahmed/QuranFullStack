namespace QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

public sealed record MushafAyahDoorsResponse(
    string VerseKey,
    IReadOnlyList<int> DoorIds);
