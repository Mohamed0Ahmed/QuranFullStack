namespace QuranDashboard.Infrastructure.Caching.Quran.MushafReader;

/// <summary>
/// Stable cache keys for immutable Mushaf reader responses (see data-model §E).
/// </summary>
/// <remarks>
/// Cache policy: all Mushaf reader entries are cached without expiration or size
/// bound. This is a deliberate "cache forever" choice because the underlying Quran
/// data is immutable between imports. <b>Operating assumption:</b> a re-import does
/// not invalidate the in-memory cache, so cached reads stay stale until the process
/// is restarted. Deployments must therefore restart the API host after any import
/// (the standard release path). Before scaling to many users, revisit this with a
/// SizeLimit + per-entry Size, or add explicit cache invalidation on import.
/// </remarks>
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
