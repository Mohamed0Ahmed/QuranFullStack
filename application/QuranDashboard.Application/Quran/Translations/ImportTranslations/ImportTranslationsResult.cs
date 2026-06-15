using QuranDashboard.Application.Abstractions.Quran.Translations;

namespace QuranDashboard.Application.Quran.Translations.ImportTranslations;

public sealed record ImportTranslationsResult(
    bool Succeeded,
    int ExitCode,
    string Message,
    TranslationImportTotals? Totals,
    string? ReportOutDir = null,
    int WarningCount = 0)
{
    public const int SuccessExitCode = 0;
    public const int FailureExitCode = 1;
    public const int RefusedExitCode = 2;

    public static ImportTranslationsResult Success(
        TranslationImportTotals totals,
        string reportOutDir,
        int warningCount = 0) =>
        new(true, SuccessExitCode, "Translation import completed successfully.", totals, reportOutDir, warningCount);

    public static ImportTranslationsResult Refused(string message, string? reportOutDir = null) =>
        new(false, RefusedExitCode, message, null, reportOutDir);

    public static ImportTranslationsResult Failure(string message, string? reportOutDir = null, int warningCount = 0) =>
        new(false, FailureExitCode, message, null, reportOutDir, warningCount);
}
