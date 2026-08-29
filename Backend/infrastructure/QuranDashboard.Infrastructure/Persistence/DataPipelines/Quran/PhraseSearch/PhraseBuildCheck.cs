namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed record PhraseBuildCheck(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
