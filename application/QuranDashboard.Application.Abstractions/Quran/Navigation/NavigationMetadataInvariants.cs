namespace QuranDashboard.Application.Abstractions.Quran.Navigation;

public static class NavigationMetadataInvariants
{
    public const int ExpectedJuz = 30;
    public const int ExpectedHizb = 60;
    public const int ExpectedRub = 240;
    public const int ExpectedSajda = 15;
    public const int ExpectedAyahs = 6_236;

    public const int ExpectedSajdaRequired = 4;
    public const int ExpectedSajdaOptional = 11;

    public static readonly NavigationExpectedCounts Production =
        new(ExpectedJuz, ExpectedHizb, ExpectedRub, ExpectedSajda, ExpectedAyahs);

    public const string TargetsNotEmpty =
        "Navigation metadata tables (or quran_ayahs nav columns) are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch =
        "Local navigation source package does not match manifest.json.";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired =
        "Navigation import passed validation, but required reports could not be written; no navigation changes were accepted.";

    public const string CheckPackageShape = "NAV-PACKAGE-SHAPE";
    public const string CheckManifestFinal = "NAV-MANIFEST-FINAL";
    public const string CheckSourceCount = "NAV-SOURCE-COUNT";
    public const string CheckSourceHash = "NAV-SOURCE-HASH";
    public const string CheckJsonShape = "NAV-JSON-SHAPE";
    public const string CheckVerseKeysResolve = "NAV-VERSE-KEYS-RESOLVE";
    public const string CheckRangeCoverageJuz = "NAV-RANGE-COVERAGE-JUZ";
    public const string CheckRangeCoverageHizb = "NAV-RANGE-COVERAGE-HIZB";
    public const string CheckRangeCoverageRub = "NAV-RANGE-COVERAGE-RUB";
    public const string CheckNoRangeGapsOverlaps = "NAV-NO-RANGE-GAPS-OVERLAPS";
    public const string CheckHierarchy = "NAV-HIERARCHY";
    public const string CheckSajdaType = "NAV-SAJDA-TYPE";
    public const string CheckAyahColumnsComplete = "NAV-AYAH-COLUMNS-COMPLETE";
    public const string CheckNoQuranTextCopy = "NAV-NO-QURAN-TEXT-COPY";
    public const string CheckSourceUnchanged = "NAV-SOURCE-UNCHANGED";
    public const string CheckReportWritten = "NAV-REPORT-WRITTEN";
    public const string CheckRollbackOnFail = "NAV-ROLLBACK-ON-FAIL";
    public const string CheckRerunGuard = "NAV-RERUN-GUARD";

    public const string WarningVerseCountMatch = "NAV-VERSE-COUNT-MATCH";
    public const string WarningSajdaDistribution = "NAV-SAJDA-DISTRIBUTION";
}

public sealed record NavigationExpectedCounts(int Juz, int Hizb, int Rub, int Sajda, int Ayahs);
