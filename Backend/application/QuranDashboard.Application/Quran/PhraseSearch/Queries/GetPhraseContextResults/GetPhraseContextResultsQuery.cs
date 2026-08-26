namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextResults;

public sealed record GetPhraseContextResultsQuery(
    string? Resolution,
    string? Previous,
    string? Following,
    int? PageSize);
