namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    short PageNumber,
    IReadOnlyList<RootAyahWordDto> Words);

public sealed record RootAyahWordDto(
    string TextUthmani,
    bool IsMatched);
