using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

public static class RootsCacheKeys
{
    public static string SummaryAll => "roots:summary:all";

    public static string Summary(int id) => $"roots:{id}:summary";

    public static string WordsAll(int id, RootWordKind kind) =>
        $"roots:{id}:words:{KindKey(kind)}:all";

    public static string Ayahs(int id, int page, int pageSize, string? typeCode) =>
        $"roots:{id}:ayahs:{NormalizeTypeCode(typeCode)}:p{page}:s{pageSize}";

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

    private static string NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? "all" : typeCode.Trim();
}
