namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    int AyahNumber,
    short PageNumber,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<AyahWordForHighlightDto> Words);

public sealed record AyahWordForHighlightDto(
    int QuranWordId,
    string TextUthmani,
    bool IsAyahMarker);
