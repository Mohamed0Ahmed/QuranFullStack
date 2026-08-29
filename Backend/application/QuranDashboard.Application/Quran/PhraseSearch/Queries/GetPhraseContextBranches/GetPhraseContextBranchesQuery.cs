namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextBranches;

public sealed record GetPhraseContextBranchesQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    string? PreviousAlternatives,
    string? FollowingAlternatives,
    string? PreviousCursor,
    string? FollowingCursor,
    int? PreviousPageSize,
    int? FollowingPageSize);
