namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseIndexGenerationState(
    bool? Persisted,
    bool? ExactReady,
    bool? SimilarityReady)
{
    internal static PhraseIndexGenerationState NotPersisted { get; } = new(false, false, false);
    internal static PhraseIndexGenerationState Unknown { get; } = new(null, null, null);

    internal bool IsAbsentAndNotReady =>
        Persisted is false && ExactReady is false && SimilarityReady is false;
}
