namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemSurahsResponse(
    IReadOnlyList<StemSurahItemDto> Surahs);

public sealed record StemSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
