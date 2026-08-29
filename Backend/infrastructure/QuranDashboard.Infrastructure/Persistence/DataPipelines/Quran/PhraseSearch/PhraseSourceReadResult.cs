namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseSourceReadResult(
    IReadOnlyList<PhraseSourceToken> Tokens,
    int AyahCount,
    short MaximumAyahLength,
    IReadOnlyList<PhraseBuildCheck> Checks)
{
    internal bool Passed => Checks
        .Where(check => string.Equals(check.Severity, "hard", StringComparison.Ordinal))
        .All(check => check.Passed);
}
