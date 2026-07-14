using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

public sealed record StemSummaryDto(
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
    int TashkeelWordsCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
