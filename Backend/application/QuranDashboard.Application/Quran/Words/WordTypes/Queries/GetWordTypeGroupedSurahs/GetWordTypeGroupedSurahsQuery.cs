namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

public sealed record GetWordTypeGroupedSurahsQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice);
