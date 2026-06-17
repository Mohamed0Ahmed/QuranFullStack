namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public sealed class FullI3rabValidationException : Exception
{
    public FullI3rabValidationException(
        IReadOnlyList<FullI3rabCheckResult> checks,
        string? message = null)
        : base(message ?? BuildMessage(checks))
    {
        Checks = checks;
        FailedChecks = checks
            .Where(check =>
                check.Severity == FullI3rabImportConstants.HardSeverity
                && !check.Passed)
            .ToList();
    }

    public IReadOnlyList<FullI3rabCheckResult> Checks { get; }

    public IReadOnlyList<FullI3rabCheckResult> FailedChecks { get; }

    private static string BuildMessage(IReadOnlyList<FullI3rabCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == FullI3rabImportConstants.HardSeverity
                && !check.Passed)
            .Select(check => check.Id)
            .ToList();

        return failed.Count == 0
            ? "Full i'rab validation failed."
            : $"Full i'rab validation failed: {string.Join(", ", failed)}.";
    }
}
