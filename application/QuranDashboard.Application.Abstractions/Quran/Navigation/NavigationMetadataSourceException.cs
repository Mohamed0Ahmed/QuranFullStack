namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public sealed class NavigationMetadataSourceException : Exception
{
    public NavigationMetadataSourceException(string message)
        : base(message)
    {
        Checks = [];
        FailedChecks = [];
    }

    public NavigationMetadataSourceException(
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
            ? "Navigation source validation failed."
            : $"Navigation source validation failed: {string.Join(", ", failed)}.";
    }
}
