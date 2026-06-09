
using QuranDashboard.Application.Abstractions.Quran.Import;

namespace QuranDashboard.Application.Quran.Import.Validation;

internal static class ImportValidationWarnings
{
    public const string Ayah37130CanonicalMessage =
        "Ayah 37:130 word-count differs (metadata 4 vs records 3); word records treated as canonical.";

    public static void CollectFromChecks(IReadOnlyList<ImportCheckResult> checks, IList<string> warnings)
    {
        foreach (var check in checks.Where(check => check.Severity == ImportValidationSeverities.Warning))
        {
            if (check.Passed && check.Id == ImportValidationCheckIds.Ayah37130Count)
            {
                warnings.Add(Ayah37130CanonicalMessage);
            }
            else if (!check.Passed)
            {
                warnings.Add($"{check.Id}: expected {check.Expected}, observed {check.Observed}");
            }
        }
    }
}
