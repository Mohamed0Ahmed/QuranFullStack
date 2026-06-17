namespace QuranDashboard.Application.Abstractions.Quran.FullI3rab;

public static class FullI3rabImportConstants
{
    public const string PassVerdict = "pass";
    public const string FailVerdict = "fail";
    public const string HardSeverity = "hard";
    public const string WarningSeverity = "warning";
    public const string InfoSeverity = "info";

    public const string ManifestType = "quran-full-i3rab-import-source-package";

    public const string ResourceKind = "full_i3rab";
    public const string MarkupFormatHtml = "html";
    public const string LanguageCode = "ar";
    public const string DirectionRtl = "rtl";

    public const string LicenseStatus = "unknown";
    public const string ProvenanceStatus = "unknown";
    public const string UsageScope = "internal-only-until-cleared";

    public const string SourceShapeGroupedLeader = "grouped_leader";
    public const string SourceShapeFlat = "flat";

    public const string ValueKindLeader = "leader";
    public const string ValueKindMemberPointer = "member_pointer";
    public const string ValueKindFlat = "flat";

    public const string MarkdownReportFileName = "full-i3rab-import-report.md";
    public const string JsonReportFileName = "full-i3rab-import-report.json";

    public const string ProvenanceWarning =
        "License/provenance is unknown and usage scope is internal-only-until-cleared. " +
        "Not cleared for public distribution — internal use only until cleared.";
}
