namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;

public sealed record GetWordTypeSummaryQuery(
    int TashkeelWordId,
    string? ContextCode,
    string? Case,
    string? Tense,
    string? Voice);
