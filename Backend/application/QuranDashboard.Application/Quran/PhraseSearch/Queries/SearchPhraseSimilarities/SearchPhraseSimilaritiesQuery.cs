namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.SearchPhraseSimilarities;

public sealed record SearchPhraseSimilaritiesQuery(
    string? ResolutionRef,
    int? MinimumMatchedWords,
    int? Page,
    int? PageSize);
