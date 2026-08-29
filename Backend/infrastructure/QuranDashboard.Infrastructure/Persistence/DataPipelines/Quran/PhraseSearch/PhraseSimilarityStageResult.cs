namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSimilarityStageResult(
    long EdgeCount,
    long AnchorStatCount,
    IReadOnlyList<PhraseLengthBuildMetric> Metrics);
