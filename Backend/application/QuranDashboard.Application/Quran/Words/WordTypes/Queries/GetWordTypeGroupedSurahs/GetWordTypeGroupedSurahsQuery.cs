namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

// Single-shot: the scoped mentioned/missing surahs for a grouped root/stem/lemma. Carries the identical
// five-field scope as the table row, with no paging (mirrors the summary contract, not the paged views).
public sealed record GetWordTypeGroupedSurahsQuery(
    string? Kind,
    int DimensionId,
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice);
