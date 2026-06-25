using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Ayah occurrence of a stem with exact matched Quran word IDs for highlighting
/// and the existing shared highlight word shape. Reuses
/// <see cref="AyahWordForHighlightDto"/>.
/// </summary>
public sealed record StemAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);
