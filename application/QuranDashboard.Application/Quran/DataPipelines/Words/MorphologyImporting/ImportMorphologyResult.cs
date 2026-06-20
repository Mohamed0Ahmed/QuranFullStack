using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

namespace QuranDashboard.Application.Quran.DataPipelines.Words.MorphologyImporting;

public sealed record ImportMorphologyResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    MorphologyImportTotals? Totals,
    string? ReportOutDir = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportMorphologyResult Success(MorphologyImportTotals totals, string reportOutDir) =>
        new(true, SuccessExitCode, "Morphology import completed successfully.", totals, reportOutDir);

    public static ImportMorphologyResult Refused(string message) =>
        new(false, RefusedExitCode, message, null, null);

    public static ImportMorphologyResult Failure(string message, string? reportOutDir = null) =>
        new(false, FailureExitCode, message, null, reportOutDir);
}
