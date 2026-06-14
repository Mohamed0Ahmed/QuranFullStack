using QuranDashboard.Application.Abstractions.Quran.Tafsirs;

namespace QuranDashboard.Infrastructure.Files.Quran.Tafsirs;

internal static class TafsirValidationChecks
{
    public static TafsirCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, TafsirImportConstants.HardSeverity, expected, observed, passed);

    public static void EnsureAllHardChecksPassed(IReadOnlyList<TafsirCheckResult> checks)
    {
        if (checks.Any(check =>
                check.Severity == TafsirImportConstants.HardSeverity
                && !check.Passed))
        {
            throw new TafsirValidationException(checks);
        }
    }
}
