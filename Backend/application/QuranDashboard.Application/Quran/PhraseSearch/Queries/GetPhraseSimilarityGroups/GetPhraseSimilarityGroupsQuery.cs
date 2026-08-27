namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityGroups;

public sealed record GetPhraseSimilarityGroupsQuery(
    string? Mode,
    int? WordCount,
    int? Threshold,
    string? Sort,
    int? Page,
    int? PageSize);
