namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Tafsirs;

public static class TafsirImportConstants
{
    public const string PassVerdict = "pass";
    public const string FailVerdict = "fail";
    public const string HardSeverity = "hard";
    public const string WarningSeverity = "warning";
    public const string InfoSeverity = "info";

    public const string ManifestType = "quran-tafsir-import-source-package";

    public const string SourceShapeGroupedLeader = "grouped_leader";
    public const string SourceShapeFlat = "flat";

    public const string ValueKindLeader = "leader";
    public const string ValueKindMemberPointer = "member_pointer";
    public const string ValueKindFlat = "flat";

    public const string MarkdownReportFileName = "tafsir-import-report.md";
    public const string JsonReportFileName = "tafsir-import-report.json";
}
