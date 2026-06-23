using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words;

public static class UniqueWordsCacheKeys
{
    public static string List(UniqueWordKind kind, UniqueWordSort sort, int page, int pageSize) =>
        $"words:{KindKey(kind)}:list:{SortKey(sort)}:p{page}:s{pageSize}";

    public static string Summary(UniqueWordKind kind, int id) =>
        $"words:{KindKey(kind)}:{id}:summary";

    public static string Surahs(UniqueWordKind kind, int id) =>
        $"words:{KindKey(kind)}:{id}:surahs";

    public static string Missing(UniqueWordKind kind, int id) =>
        $"words:{KindKey(kind)}:{id}:missing";

    public static string Ayahs(UniqueWordKind kind, int id, int page, int pageSize) =>
        $"words:{KindKey(kind)}:{id}:ayahs:p{page}:s{pageSize}";

    private static string KindKey(UniqueWordKind kind) => kind switch
    {
        UniqueWordKind.Tashkeel => UniqueWordKindKeys.Tashkeel,
        UniqueWordKind.Simple => UniqueWordKindKeys.Simple,
        _ => kind.ToString(),
    };

    private static string SortKey(UniqueWordSort sort) => sort switch
    {
        UniqueWordSort.MushafOrder => UniqueWordSortKeys.MushafOrder,
        UniqueWordSort.Occurrences => UniqueWordSortKeys.Occurrences,
        UniqueWordSort.Alpha => UniqueWordSortKeys.Alpha,
        _ => sort.ToString(),
    };
}
