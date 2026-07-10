namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.SimpleI3rabGeneration;

public sealed record I3rabCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
