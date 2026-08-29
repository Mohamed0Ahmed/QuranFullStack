namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitions;

public sealed record GetPhraseRepetitionsQuery(
    string? Mode,
    int? WordCount,
    string? Q64,
    string? Sort,
    int? Page,
    int? PageSize);
