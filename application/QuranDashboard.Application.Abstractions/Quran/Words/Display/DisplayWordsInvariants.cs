namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public static class DisplayWordsInvariants
{
    public const int ExpectedReadableWords = 77_432;
    public const int CanonicalTotalQuranWordRows = 83_668;
    public const int CanonicalReadableWords = ExpectedReadableWords;
    public const int CanonicalAyahMarkers = 6_236;
    public const int CanonicalOrderedTashkeel = 77_432;
    public const int CanonicalOrderedSimple = 77_432;
    public const int CanonicalUniqueTashkeel = 21_294;
    public const int CanonicalUniqueSimple = 14_783;
    public const int InformationalUniqueTashkeel = 21_210;
    public const int InformationalUniqueSimple = CanonicalUniqueSimple;

    public const string TargetsNotEmpty =
        "Display word tables are not empty. Re-run with --force to truncate and rebuild them.";
}
