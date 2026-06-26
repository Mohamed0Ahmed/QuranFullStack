using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

/// <summary>
/// Stem summary. Same identity/display/count fields as
/// <see cref="StemListItemDto"/>, plus the full ordered type distribution.
/// </summary>
public sealed record StemSummaryDto(
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
    string FirstVerseKey,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
