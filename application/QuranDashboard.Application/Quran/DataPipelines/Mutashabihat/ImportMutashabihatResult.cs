using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Mutashabihat;

namespace QuranDashboard.Application.Quran.DataPipelines.Mutashabihat;

public sealed record ImportMutashabihatResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    MutashabihatImportTotals? Totals,
    string? ReportOutDir = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportMutashabihatResult Success(MutashabihatImportTotals totals, string reportOutDir) =>
        new(true, SuccessExitCode, "Mutashabihat import completed successfully.", totals, reportOutDir);

    public static ImportMutashabihatResult Refused(string message) =>
        new(false, RefusedExitCode, message, null, null);

    public static ImportMutashabihatResult Failure(string message, string? reportOutDir = null) =>
        new(false, FailureExitCode, message, null, reportOutDir);
}
