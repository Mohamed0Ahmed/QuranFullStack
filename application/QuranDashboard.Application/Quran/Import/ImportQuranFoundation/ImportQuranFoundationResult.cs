
namespace QuranDashboard.Application.Quran.Import.ImportQuranFoundation;

public sealed record ImportQuranFoundationResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    ImportTotals? Totals)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportQuranFoundationResult Success(ImportTotals totals) =>
        new(true, SuccessExitCode, "Import completed successfully.", totals);

    public static ImportQuranFoundationResult Refused(string message) =>
        new(false, RefusedExitCode, message, null);

    public static ImportQuranFoundationResult Failure(string message) =>
        new(false, FailureExitCode, message, null);
}
