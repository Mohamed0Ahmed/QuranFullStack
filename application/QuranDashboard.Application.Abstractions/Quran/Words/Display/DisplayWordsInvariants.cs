namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public static class DisplayWordsInvariants
{
    public const int ExpectedReadableWords = 77_432;
    public const int InformationalUniqueTashkeel = 21_210;
    public const int InformationalUniqueSimple = 14_783;

    public const string TargetsNotEmpty =
        "Display word tables are not empty. Re-run with --force to truncate and rebuild them.";
}
