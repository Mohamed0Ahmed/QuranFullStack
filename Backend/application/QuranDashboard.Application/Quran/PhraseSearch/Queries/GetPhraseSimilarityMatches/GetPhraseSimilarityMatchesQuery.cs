namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityMatches;

public sealed record GetPhraseSimilarityMatchesQuery(
    Guid BuildId,
    long VariantId,
    int? Threshold,
    int? Page,
    int? PageSize);
