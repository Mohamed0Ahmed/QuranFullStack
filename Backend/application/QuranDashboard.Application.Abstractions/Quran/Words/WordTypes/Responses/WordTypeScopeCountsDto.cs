namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

// Each count is byte-consistent with the corresponding tableView's TotalCount for the identical scope
// (FR-016). Scoped word-context family only — never the global words_count-backed family.
public sealed record WordTypeScopeCountsDto(
    int WordsCount,
    int RootsCount,
    int StemsCount,
    int LemmasCount);
