namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Mentioned surahs for a lemma — distinct matching surahs with scoped
/// occurrence counts, ordered by Mushaf order. Mentioned and missing sets are
/// disjoint; their union is all 114 surahs.
/// </summary>
public sealed record LemmaSurahsResponse(
    int Id,
    string LemmaText,
    int SurahsCount,
    IReadOnlyList<LemmaSurahItemDto> Surahs);

public sealed record LemmaSurahItemDto(
    int SurahNumber,
    string NameArabic,
    int OccurrencesInSurah);
