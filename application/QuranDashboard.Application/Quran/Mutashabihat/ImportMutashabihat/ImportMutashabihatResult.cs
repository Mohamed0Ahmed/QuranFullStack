using QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

namespace QuranDashboard.Application.Quran.Mutashabihat.ImportMutashabihat;

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

    public static ImportMutashabihatResult Success(MutashabihatImportTotals totals) =>
        new(true, SuccessExitCode, "Mutashabihat import completed successfully.", totals, null);

    public static ImportMutashabihatResult Refused(string message) =>
        new(false, RefusedExitCode, message, null, null);

    public static ImportMutashabihatResult Failure(string message) =>
        new(false, FailureExitCode, message, null, null);
}
