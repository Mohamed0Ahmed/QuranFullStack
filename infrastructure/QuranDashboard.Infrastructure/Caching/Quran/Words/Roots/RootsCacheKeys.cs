using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

/// <summary>
/// Cache keys for the Roots Explorer over the shared <c>IMemoryCache</c>, using
/// a dedicated <c>roots:</c> namespace. Mirrors Feature 014
/// <c>UniqueWordsCacheKeys</c>.
/// </summary>
public static class RootsCacheKeys
{
    /// <summary>The compute-once whole summary the list page is derived from.</summary>
    public static string SummaryAll => "roots:summary:all";

    public static string Summary(int id) => $"roots:{id}:summary";

    public static string Words(int id, RootWordKind kind, int page, int pageSize) =>
        $"roots:{id}:words:{KindKey(kind)}:p{page}:s{pageSize}";

    public static string Ayahs(int id, int page, int pageSize) =>
        $"roots:{id}:ayahs:p{page}:s{pageSize}";

    public static string Surahs(int id) => $"roots:{id}:surahs";

    public static string Missing(int id) => $"roots:{id}:missing";

    public static string Lemmas(int id) => $"roots:{id}:lemmas";

    public static string Stems(int id) => $"roots:{id}:stems";

    private static string KindKey(RootWordKind kind) => kind switch
    {
        RootWordKind.Simple => RootWordKindKeys.Simple,
        RootWordKind.Tashkeel => RootWordKindKeys.Tashkeel,
        _ => kind.ToString(),
    };
}
