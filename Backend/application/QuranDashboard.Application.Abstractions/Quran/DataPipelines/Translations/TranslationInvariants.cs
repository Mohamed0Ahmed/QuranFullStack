namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Translations;

public static class TranslationInvariants
{
    public const int CuratedTenApprovedSources = 10;
    public const int CuratedTenSimpleSources = 9;
    public const int CuratedTenWithFootnotesSources = 1;
    public const int CuratedTenExcludedSources = 0;
    public const int CuratedTenLanguageCount = 10;
    public const int CuratedTenSourceAyahMappings = 62_360;

    public const int ExpectedApprovedSources = 167;
    public const int ExpectedSimpleSources = 129;
    public const int ExpectedWithFootnotesSources = 38;
    public const int ExpectedExcludedSources = 19;
    public const int ExpectedLanguageCount = 83;
    public const int ExpectedAyahsPerSource = 6_236;
    public const int ExpectedSourceAyahMappings = 1_041_412;

    public static readonly TranslationExpectedCounts CuratedTen = new(
        CuratedTenApprovedSources,
        CuratedTenSimpleSources,
        CuratedTenWithFootnotesSources,
        CuratedTenExcludedSources,
        CuratedTenLanguageCount,
        ExpectedAyahsPerSource,
        CuratedTenSourceAyahMappings);

    public static readonly TranslationExpectedCounts Production = new(
        ExpectedApprovedSources,
        ExpectedSimpleSources,
        ExpectedWithFootnotesSources,
        ExpectedExcludedSources,
        ExpectedLanguageCount,
        ExpectedAyahsPerSource,
        ExpectedSourceAyahMappings);

    public const string TargetsNotEmpty =
        "Translation tables are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch =
        "Local translation source package does not match manifest.json.";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired =
        "Translation import passed validation, but required reports could not be written; no translation changes were accepted.";

    public const string CheckPackageShape = "TR-PACKAGE-SHAPE";
    public const string CheckManifestFinal = "TR-MANIFEST-FINAL";
    public const string CheckDisplayMetadataFinal = "TR-DISPLAY-METADATA-FINAL";
    public const string CheckDisplayMetadataSet = "TR-DISPLAY-METADATA-SET";
    public const string CheckDisplayMetadataRequiredFields = "TR-DISPLAY-METADATA-REQUIRED-FIELDS";
    public const string CheckSourceCount = "TR-SOURCE-COUNT";
    public const string CheckTypeCounts = "TR-TYPE-COUNTS";
    public const string CheckExcludedCount = "TR-EXCLUDED-COUNT";
    public const string CheckSourceSet = "TR-SOURCE-SET";
    public const string CheckSourceHash = "TR-SOURCE-HASH";
    public const string CheckNoExcludedSources = "TR-NO-EXCLUDED-SOURCES";
    public const string CheckJsonShape = "TR-JSON-SHAPE";
    public const string CheckCoverageCount = "TR-COVERAGE-COUNT";
    public const string CheckNoEmptyText = "TR-NO-EMPTY-TEXT";
    public const string CheckAyahKeysResolve = "TR-AYAH-KEYS-RESOLVE";
    public const string CheckNoDuplicateAyahEntry = "TR-NO-DUPLICATE-AYAH-ENTRY";
    public const string CheckTextUnchanged = "TR-TEXT-UNCHANGED";
    public const string CheckNoQuranTextCopy = "TR-NO-QURAN-TEXT-COPY";
    public const string CheckPostCopySourceRows = "TR-POSTCOPY-SOURCE-ROWS";
    public const string CheckPostCopyAyahMappings = "TR-POSTCOPY-AYAH-MAPPINGS";
    public const string CheckSourceUnchanged = "TR-SOURCE-UNCHANGED";
    public const string CheckReportWritten = "TR-REPORT-WRITTEN";
    public const string CheckRollbackOnFail = "TR-ROLLBACK-ON-FAIL";
    public const string CheckRerunGuard = "TR-RERUN-GUARD";

    public const string WarningProvenance = "TR-PROVENANCE-WARNING";
    public const string InfoInlineMarkup = "TR-INLINE-MARKUP";
    public const string InfoLanguageCoverage = "TR-LANGUAGE-COVERAGE";
    public const string InfoReclassified = "TR-RECLASSIFIED";
}

public sealed record TranslationExpectedCounts(
    int ApprovedSources,
    int SimpleSources,
    int WithFootnotesSources,
    int ExcludedSources,
    int Languages,
    int AyahsPerSource,
    int SourceAyahMappings);
