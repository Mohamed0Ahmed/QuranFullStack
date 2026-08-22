using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;

// Keys are bounded by resource identity, view, kind, page, and size; raw free-text search is never part
// of a retained key, keeping the key space bounded.
public static class LemmasCacheKeys
{
    public static string SummaryAll => "lemmas:summary:all";

    public static string Summary(int id) => $"lemmas:{id}:summary";

    public static string WordsAll(int id, LemmaWordKind kind, string? typeCode) =>
        $"lemmas:{id}:words:{KindKey(kind)}:{NormalizeTypeCode(typeCode)}:all";

    public static string Words(int id, LemmaWordKind kind, string? typeCode, int page, int pageSize) =>
        $"lemmas:{id}:words:{KindKey(kind)}:{NormalizeTypeCode(typeCode)}:p{page}:s{pageSize}";

    public static string Ayahs(int id, int page, int pageSize, string? typeCode) =>
        $"lemmas:{id}:ayahs:{NormalizeTypeCode(typeCode)}:p{page}:s{pageSize}";

    public static string Surahs(int id) => $"lemmas:{id}:surahs";

    public static string Missing(int id) => $"lemmas:{id}:missing";

    public static string Stems(int id) => $"lemmas:{id}:stems";

    private static string KindKey(LemmaWordKind kind) => kind switch
    {
        LemmaWordKind.Simple => LemmaWordKindKeys.Simple,
        LemmaWordKind.Tashkeel => LemmaWordKindKeys.Tashkeel,
        _ => kind.ToString(),
    };

    private static string NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? "all" : typeCode.Trim();
}
