namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed record DisplayWordsCheckResult(
    string Id,
    string Severity,
    string Expected,
    string Observed,
    bool Passed);
