namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;

public sealed record GetWordTypeGroupedAyahsQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    int Page,
    int PageSize);
