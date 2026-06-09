namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public sealed record DisplayWordsCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
