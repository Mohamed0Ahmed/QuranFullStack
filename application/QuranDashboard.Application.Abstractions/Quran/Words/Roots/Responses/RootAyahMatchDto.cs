using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

/// <summary>
/// A verse containing a root's words. <see cref="MatchedQuranWordIds"/> holds
/// the exact <c>quran_words.id</c> values for the root within this ayah, used
/// for ID-based highlighting (never string replacement). Reuses the Feature 014
/// <see cref="AyahWordForHighlightDto"/> for the ordered word rendering.
/// </summary>
public sealed record RootAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);
