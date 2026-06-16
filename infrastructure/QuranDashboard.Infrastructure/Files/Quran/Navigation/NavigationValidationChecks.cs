using QuranDashboard.Application.Abstractions.Quran.Navigation;

namespace QuranDashboard.Infrastructure.Files.Quran.Navigation;

internal static class NavigationValidationChecks
{
    public static NavigationCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, NavigationImportConstants.HardSeverity, expected, observed, passed);

    public static NavigationCheckResult Warning(string id, string expected, string observed, bool passed) =>
        new(id, NavigationImportConstants.WarningSeverity, expected, observed, passed);

    public static void EnsureAllHardChecksPassed(IReadOnlyList<NavigationCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == NavigationImportConstants.HardSeverity
                && !check.Passed)
            .ToList();

        if (failed.Count > 0)
        {
            throw new NavigationMetadataValidationException(failed);
        }
    }
}
