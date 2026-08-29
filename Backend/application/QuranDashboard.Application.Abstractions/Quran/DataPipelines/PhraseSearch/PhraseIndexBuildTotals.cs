namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public sealed record PhraseIndexBuildTotals(
    long SearchTokens,
    long Variants,
    long Occurrences,
    long SimilarityEdges,
    long SimilarityAnchorStats)
{
    public static PhraseIndexBuildTotals Empty { get; } = new(0, 0, 0, 0, 0);
}
