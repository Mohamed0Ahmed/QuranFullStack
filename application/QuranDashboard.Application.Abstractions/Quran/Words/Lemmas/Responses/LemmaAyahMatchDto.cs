using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Ayah occurrence of a lemma with exact matched Quran word IDs for highlighting
/// and the existing shared highlight word shape. Reuses
/// <see cref="AyahWordForHighlightDto"/>.
/// </summary>
public sealed record LemmaAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);
