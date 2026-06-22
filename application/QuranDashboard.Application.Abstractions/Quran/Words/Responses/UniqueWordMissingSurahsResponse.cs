namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// Surahs where a selected unique word does NOT appear: the 114-surah
/// catalog minus the selected word's mentioned-surah set. Ordered by surah
/// number. Returns an empty <see cref="Surahs"/> list when the word appears
/// in all surahs.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D4.</remarks>
public sealed record UniqueWordMissingSurahsResponse(
    int Id,
    string Kind,
    string DisplayTextUthmani,
    int MissingSurahsCount,
    IReadOnlyList<MissingSurahItemDto> Surahs);

public sealed record MissingSurahItemDto(
    int SurahNumber,
    string NameArabic);
