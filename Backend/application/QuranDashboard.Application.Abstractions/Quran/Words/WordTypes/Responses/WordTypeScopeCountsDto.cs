namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

// Scoped four-count summary (Feature 026, US8): the words + distinct roots/stems/lemmas counts for the
// full active Word Types list scope (type, childCode, case, tense, voice, search, presence flags). Each
// count is byte-consistent with the corresponding tableView's PagedResult.TotalCount for the identical
// scope (FR-016), because the reader reuses the same RowsCountSql / GroupedRowsCountSql formulas over the
// shared BaseRowsSql base. Scoped word-context family only — never the global words_count-backed family.
public sealed record WordTypeScopeCountsDto(
    int WordsCount,
    int RootsCount,
    int StemsCount,
    int LemmasCount);
