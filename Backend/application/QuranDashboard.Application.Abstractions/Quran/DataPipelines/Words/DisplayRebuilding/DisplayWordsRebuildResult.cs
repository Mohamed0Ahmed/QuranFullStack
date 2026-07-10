namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed record DisplayWordsRebuildResult(
    DateTimeOffset RunAtUtc,
    string Verdict,
    bool Persisted,
    bool Forced,
    DisplayWordsTotals Totals,
    IReadOnlyList<DisplayWordsCheckResult> Checks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> InfoNotes);
