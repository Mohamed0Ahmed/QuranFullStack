using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.DisplayRebuilding;

public sealed record RebuildDisplayWordsResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    DisplayWordsTotals? Totals,
    string? ReportOutDir = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static RebuildDisplayWordsResult Success(DisplayWordsTotals totals, string reportOutDir) =>
        new(true, SuccessExitCode, "Display words rebuild completed successfully.", totals, reportOutDir);

    public static RebuildDisplayWordsResult SuccessWithWarnings(
        DisplayWordsTotals totals,
        string reportOutDir) =>
        new(
            true,
            SuccessExitCode,
            "Display words rebuild completed and persisted successfully with warnings; see the report.",
            totals,
            reportOutDir);

    public static RebuildDisplayWordsResult Refused(string message) =>
        new(false, RefusedExitCode, message, null, null);

    public static RebuildDisplayWordsResult Failure(string message, string? reportOutDir = null) =>
        new(false, FailureExitCode, message, null, reportOutDir);
}
