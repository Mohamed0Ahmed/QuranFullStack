using QuranDashboard.Application.Abstractions.Common.Filtering;
using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words;

public static class UniqueWordsCacheKeys
{
    // An absent/empty count filter yields the pre-feature key byte-for-byte so warm entries stay
    // valid (Feature 026, US5); an active filter appends a deterministic range segment so filtered
    // and unfiltered reads never cross-serve.
    public static string List(
        UniqueWordKind kind,
        UniqueWordSort sort,
        int page,
        int pageSize,
        UniqueWordsCountFilter? filter = null)
    {
        var baseKey = $"words:{KindKey(kind)}:list:{SortKey(sort)}:p{page}:s{pageSize}";
        return filter is null || !filter.IsActive
            ? baseKey
            : $"{baseKey}:{FilterKey(filter)}";
    }

    private static string FilterKey(UniqueWordsCountFilter filter) =>
        $"occ{RangeKey(filter.Occurrences)}:ayahs{RangeKey(filter.Ayahs)}:surahs{RangeKey(filter.Surahs)}";

    private static string RangeKey(CountRange range) =>
        $"{range.Min?.ToString() ?? string.Empty}-{range.Max?.ToString() ?? string.Empty}";

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
