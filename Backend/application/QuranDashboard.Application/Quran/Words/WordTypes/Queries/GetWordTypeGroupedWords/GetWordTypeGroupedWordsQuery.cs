namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;

public sealed record GetWordTypeGroupedWordsQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    int Page,
    int PageSize);
