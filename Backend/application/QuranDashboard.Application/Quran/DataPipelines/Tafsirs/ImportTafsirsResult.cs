using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

namespace QuranDashboard.Application.Quran.DataPipelines.Tafsirs;

public sealed record ImportTafsirsResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    TafsirImportTotals? Totals,
    string? ReportOutDir = null,
    int WarningCount = 0)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportTafsirsResult Success(TafsirImportTotals totals, string reportOutDir, int warningCount = 0) =>
        new(true, SuccessExitCode, "Tafsir import completed successfully.", totals, reportOutDir, warningCount);

    public static ImportTafsirsResult Refused(string message, string? reportOutDir = null) =>
        new(false, RefusedExitCode, message, null, reportOutDir);

    public static ImportTafsirsResult Failure(string message, string? reportOutDir = null, int warningCount = 0) =>
        new(false, FailureExitCode, message, null, reportOutDir, warningCount);
}
