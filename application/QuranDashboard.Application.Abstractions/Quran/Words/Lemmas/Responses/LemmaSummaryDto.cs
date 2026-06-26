using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

/// <summary>
/// Lemma summary. Same identity/display/count fields as
/// <see cref="LemmaListItemDto"/>, plus the full ordered type distribution.
/// </summary>
public sealed record LemmaSummaryDto(
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
    string FirstVerseKey,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
