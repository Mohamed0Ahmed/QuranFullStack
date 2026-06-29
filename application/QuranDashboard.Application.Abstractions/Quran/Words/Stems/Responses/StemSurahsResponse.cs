namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Mentioned surahs for a stem — distinct matching surahs with scoped
/// occurrence counts, ordered by Mushaf order.
/// </summary>
public sealed record StemSurahsResponse(
    IReadOnlyList<StemSurahItemDto> Surahs);

public sealed record StemSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
