namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordSurahsResponse(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    int SurahsCount,
    IReadOnlyList<UniqueWordSurahItemDto> Surahs);

public sealed record UniqueWordSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
