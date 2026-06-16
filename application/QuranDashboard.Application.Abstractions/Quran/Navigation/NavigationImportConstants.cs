namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public static class NavigationImportConstants
{
    public const string FeatureId = "009-quran-navigation-metadata-foundation";

    public const string AcceptedVerdict = "accepted";
    public const string RefusedVerdict = "refused";
    public const string ValidationFailedVerdict = "validation-failed";
    public const string ReportWriteFailedVerdict = "report-write-failed";

    public const string HardSeverity = "hard";
    public const string WarningSeverity = "warning";

    public const string ManifestType = "quran-navigation-metadata-import-source-package";

    public const string JsonReportFileName = "navigation-metadata-import-report.json";
    public const string MarkdownReportFileName = "navigation-metadata-import-report.md";
}
