namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology.Irab;

public sealed record I3rabCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
