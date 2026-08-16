using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

public sealed record RootSummaryDto(
    int Id,
    string RootText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int SimpleWordsCount,
    int TashkeelWordsCount,
    int LemmasCount,
    int StemsCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
