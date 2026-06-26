namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordSurahsResponse(
    IReadOnlyList<UniqueWordSurahItemDto> Surahs);

public sealed record UniqueWordSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
