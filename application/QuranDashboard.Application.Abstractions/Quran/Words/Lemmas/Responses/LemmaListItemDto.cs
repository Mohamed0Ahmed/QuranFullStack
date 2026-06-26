using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Lemma catalogue row. Root fields come from the lemma's owned root
/// (<c>quran_lemmas.root_id</c>); all are null when the lemma has no owned root.
/// <see cref="DominantType"/> is the first ordered type-distribution entry.
/// </summary>
public sealed record LemmaListItemDto(
    int Id,
    string LemmaText,
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
    int StemsCount,
    string FirstVerseKey);
