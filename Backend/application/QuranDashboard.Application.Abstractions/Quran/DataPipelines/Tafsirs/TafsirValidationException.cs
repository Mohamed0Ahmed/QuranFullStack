namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public sealed class TafsirValidationException : Exception
{
    public TafsirValidationException(
        IReadOnlyList<TafsirCheckResult> checks,
        string? message = null)
        : base(message ?? BuildMessage(checks))
    {
        Checks = checks;
        FailedChecks = checks
            .Where(check =>
                check.Severity == TafsirImportConstants.HardSeverity
                && !check.Passed)
            .ToList();
    }

    public IReadOnlyList<TafsirCheckResult> Checks { get; }

    public IReadOnlyList<TafsirCheckResult> FailedChecks { get; }

    private static string BuildMessage(IReadOnlyList<TafsirCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == TafsirImportConstants.HardSeverity
                && !check.Passed)
            .Select(check => check.Id)
            .ToList();

        return failed.Count == 0
            ? "Tafsir validation failed."
            : $"Tafsir validation failed: {string.Join(", ", failed)}.";
    }
}
