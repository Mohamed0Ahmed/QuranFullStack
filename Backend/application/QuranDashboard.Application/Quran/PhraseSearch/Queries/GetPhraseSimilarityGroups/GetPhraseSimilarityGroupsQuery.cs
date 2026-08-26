namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityGroups;

public sealed record GetPhraseSimilarityGroupsQuery(
    string? Mode,
    int? WordCount,
    int? Threshold,
    int? Page,
    int? PageSize);
