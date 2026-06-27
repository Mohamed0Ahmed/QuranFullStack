using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

public sealed record LemmaSummaryDto(
    int Id,
    string LemmaText,
    int? RootId,
    string? RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int StemsCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
