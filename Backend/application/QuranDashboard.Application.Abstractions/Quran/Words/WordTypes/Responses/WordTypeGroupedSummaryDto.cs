namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

public sealed record WordTypeGroupedSummaryDto(
    string Kind,                 // root | stem | lemma
    int DimensionId,
    string DisplayText,
    int OccurrencesCount,
    int AyahsCount,
    int SurahsCount);
