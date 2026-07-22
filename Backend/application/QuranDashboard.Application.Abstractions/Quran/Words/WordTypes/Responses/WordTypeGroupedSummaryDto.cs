namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeGroupedSummaryDto(
    string Kind,
    int DimensionId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);
