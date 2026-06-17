using QuranDashboard.Application.Abstractions.Quran.FullI3rab;

namespace QuranDashboard.Application.Quran.FullI3rab.ImportFullI3rab;

public sealed record ImportFullI3rabResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    FullI3rabImportTotals? Totals,
    string? ReportOutDir = null,
    int WarningCount = 0)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportFullI3rabResult Success(
        FullI3rabImportTotals totals,
        string? reportOutDir,
        int warningCount = 0) =>
        new(true, SuccessExitCode, "Full i'rab import completed successfully.", totals, reportOutDir, warningCount);

    public static ImportFullI3rabResult Refused(string message, string? reportOutDir = null) =>
        new(false, RefusedExitCode, message, null, reportOutDir);

    public static ImportFullI3rabResult Failure(string message, string? reportOutDir = null, int warningCount = 0) =>
        new(false, FailureExitCode, message, null, reportOutDir, warningCount);
}
