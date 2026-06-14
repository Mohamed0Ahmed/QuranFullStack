using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Application.Quran.Tafsirs.ImportTafsirs;

public sealed record ImportTafsirsResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    TafsirImportTotals? Totals,
    string? ReportOutDir = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportTafsirsResult Success(TafsirImportTotals totals, string reportOutDir) =>
        new(true, SuccessExitCode, "Tafsir import completed successfully.", totals, reportOutDir);

    public static ImportTafsirsResult Refused(string message) =>
        new(false, RefusedExitCode, message, null, null);

    public static ImportTafsirsResult Failure(string message, string? reportOutDir = null) =>
        new(false, FailureExitCode, message, null, reportOutDir);
}
