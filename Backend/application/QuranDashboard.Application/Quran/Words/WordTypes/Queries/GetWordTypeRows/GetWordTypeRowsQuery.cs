namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

public sealed record GetWordTypeRowsQuery(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search,
    string? Sort,
    int Page,
    int PageSize);
