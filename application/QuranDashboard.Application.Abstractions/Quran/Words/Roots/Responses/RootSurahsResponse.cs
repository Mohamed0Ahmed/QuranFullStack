namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootSurahsResponse(
    IReadOnlyList<RootSurahItemDto> Surahs);

public sealed record RootSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
