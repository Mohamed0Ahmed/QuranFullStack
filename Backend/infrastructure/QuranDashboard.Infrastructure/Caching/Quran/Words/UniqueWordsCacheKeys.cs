using QuranDashboard.Application.Abstractions.Common.Filtering;
using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words;

public static class UniqueWordsCacheKeys
{
    public static string List(
        UniqueWordKind kind,
        UniqueWordSortSpec sort,
        int page,
        int pageSize,
        UniqueWordsCountFilter? filter = null,
        UniqueWordsAssociationFilter? association = null)
    {
        var key = $"words:{KindKey(kind)}:list:{SortKey(sort)}:p{page}:s{pageSize}";
        if (filter is { IsActive: true })
        {
            key += $":{FilterKey(filter)}";
        }
        if (association is { IsActive: true })
        {
            key += $":{AssociationKey(association)}";
        }
        return key;
    }

    private static string FilterKey(UniqueWordsCountFilter filter) =>
        $"occ{RangeKey(filter.Occurrences)}:ayahs{RangeKey(filter.Ayahs)}:surahs{RangeKey(filter.Surahs)}";

    private static string AssociationKey(UniqueWordsAssociationFilter association) =>
        $"pt{association.NormalizedPrimaryType ?? string.Empty}:root{association.RootId?.ToString() ?? string.Empty}";

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

    private static string SortKey(UniqueWordSortSpec sort) => sort.CanonicalToken();
}
