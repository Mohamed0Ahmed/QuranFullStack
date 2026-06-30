namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;

public sealed record GetWordTypeSurahsQuery(
    int TashkeelWordId,
    string? ContextCode,
    string? Case,
    string? Tense,
    string? Voice);
