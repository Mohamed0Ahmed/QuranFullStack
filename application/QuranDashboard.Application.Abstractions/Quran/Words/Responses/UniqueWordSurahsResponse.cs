namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// Surahs where a selected unique word is mentioned, with the readable
/// occurrence count per surah. Ordered by surah number.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D3.</remarks>
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
