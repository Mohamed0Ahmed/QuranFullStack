namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;

public sealed record GetWordTypeTableQuery(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search,
    string? TableView,
    string? Sort,
    int Page,
    int PageSize);
