namespace QuranDashboard.Application.Quran.Words.GenerateI3rab;

public sealed record GenerateI3rabResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    string? ReportPath = null)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static GenerateI3rabResult Success(string message, string reportPath) =>
        new(true, SuccessExitCode, message, reportPath);

    public static GenerateI3rabResult Refused(string message, string? reportPath = null) =>
        new(false, RefusedExitCode, message, reportPath);

    public static GenerateI3rabResult Failure(string message, string? reportPath = null) =>
        new(false, FailureExitCode, message, reportPath);
}
