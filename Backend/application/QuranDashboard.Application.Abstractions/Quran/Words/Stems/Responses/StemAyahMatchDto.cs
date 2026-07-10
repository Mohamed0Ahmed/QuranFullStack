namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Ayah occurrence of a stem with simplified word highlighting flags.
/// </summary>
public sealed record StemAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    short PageNumber,
    IReadOnlyList<StemAyahWordDto> Words);

public sealed record StemAyahWordDto(
    string TextUthmani,
    bool IsMatched);
