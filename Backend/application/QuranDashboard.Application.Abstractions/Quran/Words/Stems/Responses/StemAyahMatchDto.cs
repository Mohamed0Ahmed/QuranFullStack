namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    short PageNumber,
    IReadOnlyList<StemAyahWordDto> Words);

public sealed record StemAyahWordDto(
    string TextUthmani,
    bool IsMatched);
