namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextOccurrences;

public sealed record GetPhraseContextOccurrencesQuery(
    string? Context,
    string? Cursor,
    int? PageSize);
