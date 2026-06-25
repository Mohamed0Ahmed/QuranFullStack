using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Stem catalogue row. Dominant lemma and dominant root are independent
/// co-occurrence rankings (count descending, then earliest Mushaf occurrence);
/// all related fields are null when the relationship is absent.
/// <see cref="DominantType"/> is the first ordered type-distribution entry.
/// </summary>
public sealed record StemListItemDto(
    int Id,
    string StemText,
    int? LemmaId,
    string? LemmaText,
    string? LemmaBuckwalter,
    int? RootId,
    string? RootText,
    string? RootBuckwalter,
    TypeSummaryDto DominantType,
    int OtherTypesCount,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    string FirstVerseKey);
