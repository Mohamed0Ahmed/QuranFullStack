namespace QuranDashboard.Application.Abstractions.Quran.Mutashabihat;

public static class MutashabihatInvariants
{
    public const int ExpectedGroups = 814;
    public const int ExpectedRawOccurrences = 3_558;
    public const int ExpectedStoredOccurrences = 3_557;
    public const int ExpectedSimilarSources = 1_162;
    public const int ExpectedSimilarLinks = 3_552;
    public const int ExpectedDistinctAyahs = 3_084;

    public const int ExpectedCoverageGt100 = 4;
    public const int ExpectedDuplicateOccurrence = 1;
    public const int ExpectedSourceKeyAbsent = 1;

    public const short MinScore = 50;
    public const short MaxScore = 100;

    public static readonly MutashabihatExpectedCounts Production = new(
        ExpectedGroups,
        ExpectedRawOccurrences,
        ExpectedStoredOccurrences,
        ExpectedSimilarSources,
        ExpectedSimilarLinks,
        ExpectedDistinctAyahs);

    public const string CheckManifestSet = "MUT-MANIFEST-SET";
    public const string CheckManifestChecksum = "MUT-MANIFEST-CHECKSUM";
    public const string CheckJsonShape = "MUT-JSON-SHAPE";
    public const string CheckSourceUnchanged = "MUT-SOURCE-UNCHANGED";
    public const string CheckLinkNoSelf = "MUT-LINK-NO-SELF";

    public const string CheckCoverageGt100 = "MUT-COVERAGE-GT-100";
    public const string CheckDuplicateOccurrence = "MUT-DUPLICATE-OCCURRENCE";
    public const string CheckSourceKeyAbsent = "MUT-SOURCE-KEY-ABSENT";
    public const string CheckStaleSourceCounters = "MUT-STALE-SOURCE-COUNTERS";
    public const string CheckWordRangeUpperBound = "MUT-WORD-RANGE-UPPER-BOUND";
    public const string CheckProvenanceLicenseUnknown = "MUT-PROVENANCE-LICENSE-UNKNOWN";

    public const string CheckOnewayLinks = "MUT-ONEWAY-LINKS";
    public const string CheckCrossDatasetOverlap = "MUT-CROSS-DATASET-OVERLAP";
    public const string CheckSurahCoverage = "MUT-SURAH-COVERAGE";
    public const string CheckPhraseVersesConsistency = "MUT-PHRASE-VERSES-CONSISTENCY";

    public const string TargetsNotEmpty =
        "Mutashabihat tables are not empty. Re-run with --force to truncate and rebuild them.";
    public const string SourceMismatch =
        "Local mutashabihat source files do not match manifest.json (file set / size / sha256).";
    public const string AyahsMissing =
        "quran_ayahs is empty or missing; run import-foundation first.";
}

public sealed record MutashabihatExpectedCounts(
    int Groups,
    int RawOccurrences,
    int StoredOccurrences,
    int SimilarSources,
    int SimilarLinks,
    int DistinctAyahs);
