namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

public static class MushafReaderCacheKeys
{
    public static string Page(int pageNumber) => $"mushaf:page:{pageNumber}";

    public static string AyahStudy(
        string verseKey,
        string? tafsirSourceKey,
        string? translationSourceKey,
        string? fullI3rabSourceKey) =>
        $"mushaf:ayah-study:{verseKey}:taf:{Sentinel(tafsirSourceKey)}:tr:{Sentinel(translationSourceKey)}:i3rab:{Sentinel(fullI3rabSourceKey)}";

    public static string WordAnalysis(string wordLocation) => $"mushaf:word-analysis:{wordLocation}";

    public static string SimilarAyahs(string verseKey) => $"mushaf:similar-ayahs:{verseKey}";

    public static string AyahMutashabihat(string verseKey) => $"mushaf:mutashabihat:{verseKey}";

    public static string SurahCatalog => "mushaf:surah-catalog";

    public static string StudySourceCatalog => "mushaf:study-source-catalog";

    private static string Sentinel(string? sourceKey) =>
        string.IsNullOrWhiteSpace(sourceKey) ? "none" : sourceKey;
}
