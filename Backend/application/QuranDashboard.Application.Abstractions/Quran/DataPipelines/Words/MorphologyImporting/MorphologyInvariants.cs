namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

public static class MorphologyInvariants
{
    public const int ExpectedReadableWords = 77_432;
    public const int ExpectedEmptyForms = 208;
    public const string RenderSource = "buckwalter-transliteration";
    public const string EnrichedRenderSource = "corpus_enriched_bridge";
    public const double InformationalWholeWordAgreement = 0.7983;

    public const string TargetsNotEmpty =
        "Morphology tables are not empty. Re-run with --force to truncate and rebuild them.";
    public const string SourceMismatch =
        "Local morphology source files do not match manifest.json (presence/count/size/sha256).";
    public const string FoundationNotLoaded =
        "Quran foundation data (quran_words) is missing or empty. Run import-foundation first.";

    public const string CheckSourceUnchanged = "MORPH-SOURCE-UNCHANGED";
    public const string CheckApprovedRootFallback = "MORPH-APPROVED-ROOT-FALLBACK";
    public const int ExpectedApprovedRootFallbacks = 45;
    public const string CheckWordLemmaNormalizationApplied = "MORPH-WORD-LEMMA-NORMALIZATION-APPLIED";
    public const string CheckWordLemmaShiftClean = "MORPH-WORD-LEMMA-SHIFT-CLEAN";
    public const string CheckWordLemmaReplaceValid = "MORPH-WORD-LEMMA-REPLACE-VALID";
    public const string CheckWordLemmaMissingRecoveryClean = "MORPH-WORD-LEMMA-MISSING-RECOVERY-CLEAN";
    public const string CheckWordLemmaUncertainZero = "MORPH-WORD-LEMMA-UNCERTAIN-ZERO";
    public const string CheckDimCounts = "MORPH-DIM-COUNTS";
    public const string CheckSegLemmaStemOnly = "SEG-LEMMA-ID-STEM-ONLY";
    public const string CheckSegLemmaRequiredForStem = "SEG-LEMMA-ID-REQUIRED-FOR-STEM";
    public const string CheckSegLemmaSingleStemHeadConsistent = "SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT";
    public const string CheckSegLemmaMultiStemResolves = "SEG-LEMMA-ID-MULTI-STEM-RESOLVES";
    public const string CheckSegLemmaNoFanout = "SEG-LEMMA-ID-NO-FANOUT";
    public const string CheckSegRootResolves = "SEG-ROOT-ID-RESOLVES";
    public const string CheckSegRootConsistent = "SEG-ROOT-ID-CONSISTENT";
    public const string CheckSegDimNullSafe = "SEG-DIM-NULL-SAFE";

    // Segment-level stem_id (Feature 018). Non-STEM => null; single-STEM and two-STEM primary segments
    // reuse the word head stem; two-STEM secondary segments come from the curated artifact (479 approved
    // clean-stem mappings + 4 intentional unresolved exceptions that stay null).
    public const string CheckSegStemStemOnly = "SEG-STEM-ID-STEM-ONLY";
    public const string CheckSegStemRequiredForStem = "SEG-STEM-ID-REQUIRED-FOR-STEM";
    public const string CheckSegStemHeadConsistent = "SEG-STEM-ID-HEAD-CONSISTENT";
    public const string CheckSegStemMultiStemCurated = "SEG-STEM-ID-MULTI-STEM-CURATED";
    public const string CheckSegStemResolves = "SEG-STEM-ID-RESOLVES";
    public const string CheckSegStemArtifactShape = "SEG-STEM-ID-ARTIFACT-SHAPE";

    public const int ExpectedSecondaryStemCandidates = 483;
    public const int ExpectedApprovedSecondaryStems = 479;
    public const int ExpectedUnresolvedSecondaryStems = 4;

    // The four secondary STEM segments intentionally left unresolved (stem_id stays null) this feature.
    public static readonly IReadOnlyList<string> UnresolvedSecondaryStemLocations =
    [
        "78:1:1:2",
        "86:5:3:2",
        "72:16:1:3",
        "20:94:2:3",
    ];
}
