using QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseIndexValidationResult(
    PhraseIndexBuildTotals Totals,
    IReadOnlyList<PhraseBuildCheck> Checks)
{
    internal bool Passed => Checks
        .Where(check => string.Equals(check.Severity, "hard", StringComparison.Ordinal))
        .All(check => check.Passed);
}
