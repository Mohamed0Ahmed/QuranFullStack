namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.SearchPhraseSimilarities;

public sealed record SearchPhraseSimilaritiesQuery(
    string? ResolutionRef,
    int? MinimumMatchedWords,
    string? Sort,
    int? Page,
    int? PageSize);
