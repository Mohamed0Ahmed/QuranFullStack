namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

/// <summary>
/// One ayah containing one or more exact occurrences of a selected unique
/// word. Highlighting is ID-based: <see cref="MatchedQuranWordIds"/> holds
/// the exact <c>quran_words.id</c> values for this ayah; the frontend marks
/// only those words and never relies on text replacement. Ayah markers are
/// excluded from matched IDs.
/// </summary>
/// <remarks>See Feature 014 data-model.md section D5.</remarks>
public sealed record UniqueWordAyahMatchDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    string SurahNameArabic,
    int AyahNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);

/// <summary>
/// A single Quran word inside an ayah, for display and ID-based highlighting.
/// <see cref="IsAyahMarker"/> is included for safety; markers must never be
/// highlighted as unique-word occurrences.
/// </summary>
public sealed record AyahWordForHighlightDto(
    int QuranWordId,
    int WordNumber,
    string TextUthmani,
    bool IsAyahMarker);
