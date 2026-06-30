namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;

public sealed record GetWordTypeAyahsQuery(
    int TashkeelWordId,
    string? ContextCode,
    string? Case,
    string? Tense,
    string? Voice,
    int Page,
    int PageSize);
