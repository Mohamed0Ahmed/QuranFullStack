namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

public static class FullI3rabInvariants
{
    public const int ExpectedSources = 4;
    public const int ExpectedAyahsPerSource = 6_236;
    public const int ExpectedSourceAyahMappings = ExpectedSources * ExpectedAyahsPerSource;

    public static readonly FullI3rabExpectedCounts Production = new(
        ExpectedSources,
        ExpectedAyahsPerSource);

    public const string TargetsNotEmpty =
        "Full i'rab tables are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch =
        "Local full-i'rab source package does not match manifest.json.";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired =
        "Full i'rab import passed validation, but required reports could not be written; no full-i'rab changes were accepted.";

    public const string CheckPackageShape = "FULLI3RAB-PACKAGE-SHAPE";
    public const string CheckManifestFinal = "FULLI3RAB-MANIFEST-FINAL";
    public const string CheckSourceCount = "FULLI3RAB-SOURCE-COUNT";
    public const string CheckSourceSet = "FULLI3RAB-SOURCE-SET";
    public const string CheckSourceHash = "FULLI3RAB-SOURCE-HASH";
    public const string CheckCoverageCount = "FULLI3RAB-COVERAGE-COUNT";
    public const string CheckJsonShape = "FULLI3RAB-JSON-SHAPE";
    public const string CheckAyahKeysResolve = "FULLI3RAB-AYAH-KEYS-RESOLVE";
    public const string CheckPointersResolve = "FULLI3RAB-POINTERS-RESOLVE";
    public const string CheckAyahKeysMemberMatch = "FULLI3RAB-AYAH-KEYS-MEMBER-MATCH";
    public const string CheckNoEmptyText = "FULLI3RAB-NO-EMPTY-TEXT";
    public const string CheckBlockPartition = "FULLI3RAB-BLOCK-PARTITION";
    public const string CheckHtmlAllowlist = "FULLI3RAB-HTML-ALLOWLIST";
    public const string CheckPostCopySourceRows = "FULLI3RAB-POSTCOPY-SOURCE-ROWS";
    public const string CheckPostCopyAyahMappings = "FULLI3RAB-POSTCOPY-AYAH-MAPPINGS";
    public const string CheckPostCopyCoverageSum = "FULLI3RAB-POSTCOPY-COVERAGE-SUM";
    public const string CheckPostCopyEntrySource = "FULLI3RAB-POSTCOPY-ENTRY-SOURCE";
    public const string CheckPostCopyAyahResolved = "FULLI3RAB-POSTCOPY-AYAH-RESOLVED";
    public const string CheckPostCopyNoEmptyHtml = "FULLI3RAB-POSTCOPY-NO-EMPTY-HTML";
    public const string CheckPostCopyHtmlUnchanged = "FULLI3RAB-POSTCOPY-HTML-UNCHANGED";
    public const string CheckPostCopySourceUnchanged = "FULLI3RAB-POSTCOPY-SOURCE-UNCHANGED";
    public const string CheckPostCopyDistinctAyahs = "FULLI3RAB-POSTCOPY-DISTINCT-AYAHS";
    public const string CheckReportWritten = "FULLI3RAB-REPORT-WRITTEN";

    public const string WarningProvenance = "FULLI3RAB-PROVENANCE-WARNING";

    public static readonly IReadOnlyList<string> ApprovedSourceKeys =
    [
        "muyassar",
        "jadwal",
        "daas",
        "darwish"
    ];

    public static readonly IReadOnlyCollection<string> AllowedHtmlTags =
    [
        "div",
        "p",
        "span",
        "b",
        "h3"
    ];

    public static readonly IReadOnlyCollection<string> AllowedHtmlClasses =
    [
        "ar",
        "hlt",
        "qpc-hafs"
    ];
}

public sealed record FullI3rabExpectedCounts(
    int Sources,
    int AyahsPerSource,
    long? ExpectedAyahMappingTotal = null)
{
    public long AyahMappingTotal => ExpectedAyahMappingTotal ?? (long)Sources * AyahsPerSource;
}
