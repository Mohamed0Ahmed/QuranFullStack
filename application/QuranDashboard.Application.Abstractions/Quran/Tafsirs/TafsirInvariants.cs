namespace QuranDashboard.Application.Abstractions.Quran.Tafsirs;

public static class TafsirInvariants
{
    public const int ExpectedApprovedSources = 84;
    public const int ExpectedExcludedSources = 9;
    public const int ExpectedArabicSources = 35;
    public const int ExpectedNonArabicSources = 49;
    public const int ExpectedLanguageCount = 33;
    public const int ExpectedAyahsPerSource = 6_236;
    public const int ExpectedSourceAyahMappings = 523_824;

    public static readonly TafsirExpectedCounts Production = new(
        ExpectedApprovedSources,
        ExpectedExcludedSources,
        ExpectedArabicSources,
        ExpectedNonArabicSources,
        ExpectedLanguageCount,
        ExpectedAyahsPerSource,
        ExpectedSourceAyahMappings);

    public const string TargetsNotEmpty =
        "Tafsir tables are not empty. Re-run with --force to rebuild them.";
    public const string SourceMismatch =
        "Local tafsir source package does not match manifest.json.";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
    public const string ReportRequired =
        "Tafsir import passed validation, but required reports could not be written; no tafsir changes were accepted.";

    public const string CheckPackageShape = "TAFSIR-PACKAGE-SHAPE";
    public const string CheckManifestFinal = "TAFSIR-MANIFEST-FINAL";
    public const string CheckSourceCount = "TAFSIR-SOURCE-COUNT";
    public const string CheckExcludedCount = "TAFSIR-EXCLUDED-COUNT";
    public const string CheckArabicSourceCount = "TAFSIR-ARABIC-SOURCE-COUNT";
    public const string CheckNonArabicSourceCount = "TAFSIR-NON-ARABIC-SOURCE-COUNT";
    public const string CheckSourceSet = "TAFSIR-SOURCE-SET";
    public const string CheckSourceHash = "TAFSIR-SOURCE-HASH";
    public const string CheckNoExcludedSources = "TAFSIR-NO-EXCLUDED-SOURCES";
    public const string CheckCoverageCount = "TAFSIR-COVERAGE-COUNT";
    public const string CheckJsonShape = "TAFSIR-JSON-SHAPE";
    public const string CheckAyahKeysResolve = "TAFSIR-AYAH-KEYS-RESOLVE";
    public const string CheckPointersResolve = "TAFSIR-POINTERS-RESOLVE";
    public const string CheckNoEmptyText = "TAFSIR-NO-EMPTY-TEXT";
    public const string CheckNoDuplicateAyahEntry = "TAFSIR-NO-DUPLICATE-AYAH-ENTRY";
    public const string CheckTextUnchanged = "TAFSIR-TEXT-UNCHANGED";
    public const string CheckNoQuranTextCopy = "TAFSIR-NO-QURAN-TEXT-COPY";
    public const string CheckPostCopySourceRows = "TAFSIR-POSTCOPY-SOURCE-ROWS";
    public const string CheckPostCopyAyahMappings = "TAFSIR-POSTCOPY-AYAH-MAPPINGS";
    public const string CheckSourceUnchanged = "TAFSIR-SOURCE-UNCHANGED";
    public const string CheckReportWritten = "TAFSIR-REPORT-WRITTEN";

    public const string WarningProvenance = "TAFSIR-PROVENANCE-WARNING";
    public const string WarningModernWorks = "TAFSIR-MODERN-WORKS-WARNING";
    public const string InfoInlineMarkup = "TAFSIR-INLINE-MARKUP";
    public const string InfoLanguageCoverage = "TAFSIR-LANGUAGE-COVERAGE";
    public const string InfoTextBlockCount = "TAFSIR-TEXT-BLOCK-COUNT";

    public static readonly string[] LockedExcludedSourceKeys =
    [
        "ar-wajiz",
        "ar-durr-al-manthur",
        "ar-ibn-al-qayyim",
        "ar-ibn-uthaymeen",
        "ar-baydawi",
        "ar-suddi",
        "ar-muyassar-fi-al-gharib",
        "id-saadi",
        "tr-ibn-kathir"
    ];
}

public sealed record TafsirExpectedCounts(
    int ApprovedSources,
    int ExcludedSources,
    int ArabicSources,
    int NonArabicSources,
    int Languages,
    int AyahsPerSource,
    int SourceAyahMappings);
