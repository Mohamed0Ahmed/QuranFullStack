namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;

public sealed record GetWordTypeGroupedSummaryQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice);
