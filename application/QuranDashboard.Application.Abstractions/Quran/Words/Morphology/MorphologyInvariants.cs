namespace QuranDashboard.Application.Abstractions.Quran.Words.Morphology;

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
}
