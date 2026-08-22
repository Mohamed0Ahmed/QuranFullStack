using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;

// No raw free-text search is ever folded into a retained cache key; keys stay bounded by resource
// identity, view, kind, page, and size.
public static class StemsCacheKeys
{
    public static string SummaryAll => "stems:summary:all";

    public static string Summary(int id) => $"stems:{id}:summary";

    public static string WordsAll(int id, StemWordKind kind, string? typeCode) =>
        $"stems:{id}:words:{KindKey(kind)}:{NormalizeTypeCode(typeCode)}:all";

    public static string Words(int id, StemWordKind kind, string? typeCode, int page, int pageSize) =>
        $"stems:{id}:words:{KindKey(kind)}:{NormalizeTypeCode(typeCode)}:p{page}:s{pageSize}";

    public static string Ayahs(int id, int page, int pageSize, string? typeCode) =>
        $"stems:{id}:ayahs:{StemsAyahTypeCode.CacheKeyPart(typeCode)}:p{page}:s{pageSize}";

    public static string Surahs(int id) => $"stems:{id}:surahs";

    public static string Missing(int id) => $"stems:{id}:missing";

    public static string Lemmas(int id) => $"stems:{id}:lemmas";

    private static string KindKey(StemWordKind kind) => kind switch
    {
        StemWordKind.Simple => StemWordKindKeys.Simple,
        StemWordKind.Tashkeel => StemWordKindKeys.Tashkeel,
        _ => kind.ToString(),
    };

    private static string NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? "all" : typeCode.Trim();
}
