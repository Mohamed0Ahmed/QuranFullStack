namespace QuranDashboard.Application.Quran.Words.GenerateI3rab;

public sealed record GenerateI3rabResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    string? ReportOutDir = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static GenerateI3rabResult Success(string message, string reportOutDir) =>
        new(true, SuccessExitCode, message, reportOutDir);

    public static GenerateI3rabResult Refused(string message) =>
        new(false, RefusedExitCode, message, null);

    public static GenerateI3rabResult Failure(string message, string? reportOutDir = null) =>
        new(false, FailureExitCode, message, reportOutDir);
}
