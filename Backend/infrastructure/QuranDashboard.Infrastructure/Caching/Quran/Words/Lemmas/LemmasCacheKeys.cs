using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;

public static class LemmasCacheKeys
{
    public static string SummaryAll => "lemmas:summary:all";

    public static string Summary(int id) => $"lemmas:{id}:summary";

    public static string WordsAll(int id, LemmaWordKind kind) =>
        $"lemmas:{id}:words:{KindKey(kind)}:all";

    public static string Words(int id, LemmaWordKind kind, int page, int pageSize) =>
        $"lemmas:{id}:words:{KindKey(kind)}:p{page}:s{pageSize}";

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
