namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootStemsResponse(
    int Id,
    string RootText,
    int StemsCount,
    IReadOnlyList<RootStemItemDto> Stems);

public sealed record RootStemItemDto(
    int StemId,
    string StemText,
    int OccurrencesCount);
