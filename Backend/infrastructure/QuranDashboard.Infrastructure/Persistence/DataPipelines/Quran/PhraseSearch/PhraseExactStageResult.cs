using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseExactStageResult(
    PhraseIndexBuildTotals Totals,
    IReadOnlyList<PhraseLengthBuildMetric> Metrics);
