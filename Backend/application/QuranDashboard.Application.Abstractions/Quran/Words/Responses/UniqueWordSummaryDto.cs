using QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Responses;

public sealed record UniqueWordSummaryDto(
    int Id,
    string Kind,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount,
    int MissingSurahsCount,
    IReadOnlyList<TypeSummaryDto> TypeDistribution);
