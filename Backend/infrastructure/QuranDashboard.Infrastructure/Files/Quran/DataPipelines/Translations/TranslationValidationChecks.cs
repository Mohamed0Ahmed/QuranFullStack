using QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.Translations;

internal static class TranslationValidationChecks
{
    public static TranslationCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, TranslationImportConstants.HardSeverity, expected, observed, passed);

    public static void EnsureAllHardChecksPassed(IReadOnlyList<TranslationCheckResult> checks)
    {
        var failed = checks
            .Where(check =>
                check.Severity == TranslationImportConstants.HardSeverity
                && !check.Passed)
            .ToList();

        if (failed.Count > 0)
        {
            throw new TranslationValidationException(failed);
        }
    }
}
