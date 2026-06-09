
namespace QuranDashboard.Application.Quran.Import.Validation;

internal static class ImportCheckResults
{
    public static ImportCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, ImportValidationSeverities.Hard, expected, observed, passed);

    public static ImportCheckResult Warning(string id, string expected, string observed, bool passed) =>
        new(id, ImportValidationSeverities.Warning, expected, observed, passed);

    public static ImportCheckResult Info(string id, string expected, string observed, bool passed) =>
        new(id, ImportValidationSeverities.Info, expected, observed, passed);
}
