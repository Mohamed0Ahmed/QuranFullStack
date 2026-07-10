namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaSurahsResponse(
    IReadOnlyList<LemmaSurahItemDto> Surahs);

public sealed record LemmaSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
