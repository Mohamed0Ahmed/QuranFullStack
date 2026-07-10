namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    short PageNumber,
    IReadOnlyList<LemmaAyahWordDto> Words);

public sealed record LemmaAyahWordDto(
    string TextUthmani,
    bool IsMatched);
