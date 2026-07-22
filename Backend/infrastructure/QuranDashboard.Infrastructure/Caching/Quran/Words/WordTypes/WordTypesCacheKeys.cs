using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

public static class WordTypesCacheKeys
{
    public const string Tree = "wordtypes:tree";

    public static string Rows(WordTypeFilter filter, WordTypeSortSpec sort, int page, int pageSize) =>
        $"wordtypes:rows:{HashFilter(filter)}:sort:{SortKey(sort)}:p{page}:s{pageSize}";

    public static string Table(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSortSpec sort, int page, int pageSize) =>
        $"wordtypes:table:{HashFilter(filter)}:view:{TableViewKey(tableView)}:sort:{SortKey(sort)}:p{page}:s{pageSize}";

    public static string ScopeCounts(WordTypeFilter filter) =>
        $"wordtypes:scope-counts:{HashFilter(filter)}";

    public static string Summary(WordTypeRowIdentity identity) =>
        $"wordtypes:summary:{HashIdentity(identity)}";

    public static string Ayahs(WordTypeRowIdentity identity, int page, int pageSize) =>
        $"wordtypes:ayahs:{HashIdentity(identity)}:p{page}:s{pageSize}";

    public static string Surahs(WordTypeRowIdentity identity) =>
        $"wordtypes:surahs:{HashIdentity(identity)}";

    public static string GroupedSummary(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:summary:{HashGroupedSelection(selection)}";

    public static string GroupedWords(WordTypeGroupedSelection selection, int page, int pageSize) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:words:{HashGroupedSelection(selection)}:p{page}:s{pageSize}";

    public static string GroupedAyahs(WordTypeGroupedSelection selection, int page, int pageSize) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:ayahs:{HashGroupedSelection(selection)}:p{page}:s{pageSize}";

    public static string GroupedSurahs(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:surahs:{HashGroupedSelection(selection)}";

    private static string HashGroupedSelection(WordTypeGroupedSelection selection) => HashParts(
        selection.DimensionId.ToString(CultureInfo.InvariantCulture),
        selection.Filter.Type,
        selection.Filter.ChildCode,
        selection.Filter.Case,
        selection.Filter.Tense,
        selection.Filter.Voice);

    // Labelled "search:"/"flags:" prefixes keep a crafted search value from occupying a flags scope's slot
    // and cross-serving another scope's cached rows (with the HashParts delimiter escaping below).
    private static string HashFilter(WordTypeFilter filter)
    {
        var normalizedSearch = ArabicSearchQueryNormalizer.Normalize(filter.Search);
        var parts = new List<string?> { filter.Type, filter.ChildCode, filter.Case, filter.Tense, filter.Voice };

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            parts.Add($"search:{normalizedSearch}");
        }

        if (filter.HasRoot is not null || filter.HasStem is not null || filter.HasLemma is not null)
        {
            parts.Add($"flags:{FlagKey(filter.HasRoot)}{FlagKey(filter.HasStem)}{FlagKey(filter.HasLemma)}");
        }

        return HashParts([.. parts]);
    }

    private static string FlagKey(bool? flag) => flag switch
    {
        true => "1",
        false => "0",
        _ => "_",
    };

    private static string HashIdentity(WordTypeRowIdentity identity) => HashParts(
        identity.TashkeelWordId.ToString(CultureInfo.InvariantCulture),
        identity.ContextCode,
        identity.Case,
        identity.Tense,
        identity.Voice);

    private static string HashParts(params string?[] parts)
    {
        var normalized = string.Join('|', parts.Select(EncodePart));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }

    // Escape '|' (and '\') so a part containing the delimiter can't split into extra slots and align with
    // a different combination of parts (cross-serve collision).
    private static string EncodePart(string? part) =>
        string.IsNullOrWhiteSpace(part) ? "_" : part.Trim().Replace("\\", "\\\\").Replace("|", "\\|");

    private static string TableViewKey(WordTypeTableView tableView) => tableView switch
    {
        WordTypeTableView.Words => WordTypeTableViewKeys.Words,
        WordTypeTableView.Roots => WordTypeTableViewKeys.Roots,
        WordTypeTableView.Stems => WordTypeTableViewKeys.Stems,
        WordTypeTableView.Lemmas => WordTypeTableViewKeys.Lemmas,
        _ => tableView.ToString(),
    };

    private static string SortKey(WordTypeSortSpec sort) => sort.CanonicalToken();
}
