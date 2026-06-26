namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Mentioned surahs for a stem — distinct matching surahs with scoped
/// occurrence counts, ordered by Mushaf order. Mentioned and missing sets are
/// disjoint; their union is all 114 surahs.
/// </summary>
public sealed record StemSurahsResponse(
    int Id,
    string StemText,
    int SurahsCount,
    IReadOnlyList<StemSurahItemDto> Surahs);

public sealed record StemSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
