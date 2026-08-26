namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitions;

public sealed record GetPhraseRepetitionsQuery(
    string? Mode,
    int? WordCount,
    string? Sort,
    int? Page,
    int? PageSize);
