namespace QuranDashboard.Application.Abstractions.Quran.Translations;

public sealed class TranslationSourceException : Exception
{
    public TranslationSourceException(
        IReadOnlyList<TranslationCheckResult> checks,
        string? message = null)
        : base(message ?? BuildMessage(checks))
    {
        Checks = checks;
        FailedChecks = checks
            .Where(check =>
                check.Severity == TranslationImportConstants.HardSeverity
                && !check.Passed)
            .ToList();
    }

    public IReadOnlyList<TranslationCheckResult> Checks { get; }

    public IReadOnlyList<TranslationCheckResult> FailedChecks { get; }

    private static string BuildMessage(IReadOnlyList<TranslationCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == TranslationImportConstants.HardSeverity
                && !check.Passed)
            .Select(check => check.Id)
            .ToList();

        return failed.Count == 0
            ? "Translation source validation failed."
            : $"Translation source validation failed: {string.Join(", ", failed)}.";
    }
}
