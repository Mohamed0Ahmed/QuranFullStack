using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Application.Quran.Navigation.ImportNavigationMetadata;

public sealed record ImportNavigationMetadataResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    NavigationImportTotals? Totals,
    string? ReportOutDir = null,
    int WarningCount = 0)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportNavigationMetadataResult Success(
        NavigationImportTotals totals,
        string reportOutDir,
        int warningCount = 0) =>
        new(
            true,
            SuccessExitCode,
            FormatMessage("Navigation metadata import completed successfully.", reportOutDir, warningCount),
            totals,
            reportOutDir,
            warningCount);

    public static ImportNavigationMetadataResult Refused(string message, string? reportOutDir = null) =>
        new(
            false,
            RefusedExitCode,
            FormatMessage(message, reportOutDir, warningCount: 0, firstActionableError: message),
            null,
            reportOutDir);

    public static ImportNavigationMetadataResult Failure(
        string message,
        string? reportOutDir = null,
        int warningCount = 0) =>
        new(
            false,
            FailureExitCode,
            FormatMessage(message, reportOutDir, warningCount, firstActionableError: message),
            null,
            reportOutDir,
            warningCount);

    private static string FormatMessage(
        string coreMessage,
        string? reportOutDir,
        int warningCount,
        string? firstActionableError = null)
    {
        var parts = new List<string> { coreMessage };

        if (!string.IsNullOrWhiteSpace(firstActionableError)
            && !string.Equals(coreMessage, firstActionableError, StringComparison.Ordinal))
        {
            parts.Add($"First error: {firstActionableError}");
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            parts.Add($"Report directory: {reportOutDir}");
        }

        if (warningCount > 0)
        {
            parts.Add($"Warnings: {warningCount}");
        }

        return string.Join(" ", parts);
    }
}
