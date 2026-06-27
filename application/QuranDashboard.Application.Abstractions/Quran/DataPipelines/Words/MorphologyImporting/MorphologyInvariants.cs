namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.MorphologyImporting;

public static class MorphologyInvariants
{
    public const int ExpectedReadableWords = 77_432;
    public const int ExpectedEmptyForms = 208;
    public const string RenderSource = "buckwalter-transliteration";
    public const double InformationalWholeWordAgreement = 0.7983;

    public const string TargetsNotEmpty =
        "Morphology tables are not empty. Re-run with --force to truncate and rebuild them.";
    public const string SourceMismatch =
        "Local morphology source files do not match manifest.json (presence/count/size/sha256).";
    public const string FoundationNotLoaded =
        "Quran foundation data (quran_words) is missing or empty. Run import-foundation first.";

    public const string CheckSourceUnchanged = "MORPH-SOURCE-UNCHANGED";
    public const string CheckDimCounts = "MORPH-DIM-COUNTS";
    public const string CheckSegLemmaStemOnly = "SEG-LEMMA-ID-STEM-ONLY";
    public const string CheckSegLemmaRequiredForStem = "SEG-LEMMA-ID-REQUIRED-FOR-STEM";
    public const string CheckSegLemmaSingleStemHeadConsistent = "SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT";
    public const string CheckSegLemmaMultiStemResolves = "SEG-LEMMA-ID-MULTI-STEM-RESOLVES";
    public const string CheckSegLemmaNoFanout = "SEG-LEMMA-ID-NO-FANOUT";
    public const string CheckSegRootResolves = "SEG-ROOT-ID-RESOLVES";
    public const string CheckSegRootConsistent = "SEG-ROOT-ID-CONSISTENT";
    public const string CheckSegDimNullSafe = "SEG-DIM-NULL-SAFE";
    public const string CheckSegStemIdAbsent = "SEG-STEM-ID-ABSENT";
}
