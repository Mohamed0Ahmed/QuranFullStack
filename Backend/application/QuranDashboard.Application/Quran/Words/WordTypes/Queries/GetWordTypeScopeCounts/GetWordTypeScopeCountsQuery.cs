namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

// The scope-counts query carries EXACTLY the Word Types list scope — nothing view- or page-related. The
// four counts describe the scope, not a page, so there is no tableView/sort/paging here (contract §3).
public sealed record GetWordTypeScopeCountsQuery(
    string? Type,
    string? ChildCode,
    string? Case,
    string? Tense,
    string? Voice,
    string? Search,
    bool? HasRoot = null,
    bool? HasStem = null,
    bool? HasLemma = null);
