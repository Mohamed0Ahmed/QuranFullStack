namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

// Carries EXACTLY the Word Types list scope, nothing view- or page-related: the four counts describe the
// scope, not a page, so there is no tableView/sort/paging here.
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
