using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Stem catalogue row. Dominant lemma and dominant root are independent
/// co-occurrence rankings (count descending, then earliest Mushaf occurrence);
/// all related fields are null when the relationship is absent.
/// </summary>
public sealed record StemListItemDto(
    int Id,
    string StemText,
    int? LemmaId,
    string? LemmaText,
    int? RootId,
    string? RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount);
