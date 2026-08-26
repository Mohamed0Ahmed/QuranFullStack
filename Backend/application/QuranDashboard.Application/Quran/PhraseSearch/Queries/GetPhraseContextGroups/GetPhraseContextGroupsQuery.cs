namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextGroups;

public sealed record GetPhraseContextGroupsQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    string? Cursor,
    int? PageSize);
