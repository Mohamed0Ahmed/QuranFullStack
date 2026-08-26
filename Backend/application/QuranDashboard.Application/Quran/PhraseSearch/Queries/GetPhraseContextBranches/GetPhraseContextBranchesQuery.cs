namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextBranches;

public sealed record GetPhraseContextBranchesQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    string? PreviousCursor,
    string? FollowingCursor,
    int? PreviousPageSize,
    int? FollowingPageSize);
