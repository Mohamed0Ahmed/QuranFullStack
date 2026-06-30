namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeSurahsResponse(
    IReadOnlyList<WordTypeSurahOccurrenceDto> Surahs,
    IReadOnlyList<WordTypeMissingSurahDto> MissingSurahs);

public sealed record WordTypeSurahOccurrenceDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesCount);

public sealed record WordTypeMissingSurahDto(
    int SurahNumber,
    string NameArabic);
