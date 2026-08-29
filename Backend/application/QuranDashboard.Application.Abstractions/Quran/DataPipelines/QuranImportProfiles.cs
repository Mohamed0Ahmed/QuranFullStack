namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines;

public static class QuranImportProfiles
{
    public const string CuratedTen = "curated-10";
    public const string Full = "full";

    public static bool IsSupported(string profile) =>
        string.Equals(profile, CuratedTen, StringComparison.Ordinal)
        || string.Equals(profile, Full, StringComparison.Ordinal);
}
