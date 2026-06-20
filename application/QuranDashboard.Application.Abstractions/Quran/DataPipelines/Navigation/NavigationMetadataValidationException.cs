namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Navigation;

public sealed class NavigationMetadataValidationException : Exception
{
    public NavigationMetadataValidationException(
        IReadOnlyList<NavigationCheckResult> checks,
        string? message = null)
        : base(message ?? BuildMessage(checks))
    {
        Checks = checks;
        FailedChecks = checks
            .Where(check =>
                check.Severity == NavigationImportConstants.HardSeverity
                && !check.Passed)
            .ToList();
    }

    public IReadOnlyList<NavigationCheckResult> Checks { get; }

    public IReadOnlyList<NavigationCheckResult> FailedChecks { get; }

    private static string BuildMessage(IReadOnlyList<NavigationCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == NavigationImportConstants.HardSeverity
                && !check.Passed)
            .Select(check => check.Id)
            .ToList();

        return failed.Count == 0
            ? "Navigation validation failed."
            : $"Navigation validation failed: {string.Join(", ", failed)}.";
    }
}
