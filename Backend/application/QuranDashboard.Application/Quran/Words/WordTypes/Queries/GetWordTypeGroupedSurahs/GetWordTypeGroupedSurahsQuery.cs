namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

// No paging: carries the identical five-field scope as the table row and mirrors the summary contract,
// not the paged views.
public sealed record GetWordTypeGroupedSurahsQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice);
