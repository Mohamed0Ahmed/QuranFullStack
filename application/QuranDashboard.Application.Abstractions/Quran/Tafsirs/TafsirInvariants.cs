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

    public const string CheckPostCopySourceRows = "TAFSIR-POSTCOPY-SOURCE-ROWS";
    public const string CheckPostCopyAyahMappings = "TAFSIR-POSTCOPY-AYAH-MAPPINGS";
    public const string CheckSourceUnchanged = "TAFSIR-SOURCE-UNCHANGED";
    public const string CheckReportWritten = "TAFSIR-REPORT-WRITTEN";
}

public sealed record TafsirExpectedCounts(
    int ApprovedSources,
    int ExcludedSources,
    int ArabicSources,
    int NonArabicSources,
    int Languages,
    int AyahsPerSource,
    int SourceAyahMappings);
