namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeSurahsResponse(
    IReadOnlyList<WordTypeSurahOccurrenceDto> Surahs,
    IReadOnlyList<int> MissingSurahs);

public sealed record WordTypeSurahOccurrenceDto(
    int SurahNumber,
    int OccurrencesCount);
