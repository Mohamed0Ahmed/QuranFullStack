using System.Security.Cryptography;
using System.Text;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

public static class WordTypesCacheKeys
{
    public const string Tree = "wordtypes:tree";

    public static string Rows(WordTypeFilter filter, WordTypeSort sort, int page, int pageSize) =>
        $"wordtypes:rows:{HashFilter(filter)}:sort:{SortKey(sort)}:p{page}:s{pageSize}";

    public static string Table(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSort sort, int page, int pageSize) =>
        $"wordtypes:table:{HashFilter(filter)}:view:{TableViewKey(tableView)}:sort:{SortKey(sort)}:p{page}:s{pageSize}";

    public static string Summary(WordTypeRowIdentity identity) =>
        $"wordtypes:summary:{HashIdentity(identity)}";

    public static string Ayahs(WordTypeRowIdentity identity, int page, int pageSize) =>
        $"wordtypes:ayahs:{HashIdentity(identity)}:p{page}:s{pageSize}";

    public static string Surahs(WordTypeRowIdentity identity) =>
        $"wordtypes:surahs:{HashIdentity(identity)}";

    // Grouped detail keys expose only the kind/view labels in the readable prefix; the dimension ID and
    // the full five-field scope are folded into the hash so different scopes never cross-serve. Each view
    // carries its own segment (summary vs words vs …) so views never share a prefix; paged views append
    // page/pageSize.
    public static string GroupedSummary(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:summary:{HashGroupedSelection(selection)}";

    public static string GroupedWords(WordTypeGroupedSelection selection, int page, int pageSize) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:words:{HashGroupedSelection(selection)}:p{page}:s{pageSize}";

    public static string GroupedAyahs(WordTypeGroupedSelection selection, int page, int pageSize) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:ayahs:{HashGroupedSelection(selection)}:p{page}:s{pageSize}";

    // Surahs are single-shot, so the key carries no page component (mirrors the summary key shape).
    public static string GroupedSurahs(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.ToRouteKey()}:surahs:{HashGroupedSelection(selection)}";

    private static string HashGroupedSelection(WordTypeGroupedSelection selection) => HashParts(
        selection.DimensionId.ToString(CultureInfo.InvariantCulture),
        selection.Filter.Type,
        selection.Filter.ChildCode,
        selection.Filter.Case,
        selection.Filter.Tense,
        selection.Filter.Voice);

    // Empty/absent search AND absent presence flags keep the pre-feature 5-part hash so warm rows/table
    // entries stay valid. A non-empty NORMALIZED search appends a component (same normalization the SQL
    // predicate uses); any set presence flag (Feature 026, US6) appends a distinct flag component. So
    // searched/flagged reads never cross-serve their unsearched/unflagged counterparts.
    private static string HashFilter(WordTypeFilter filter)
    {
        var normalizedSearch = ArabicSearchQueryNormalizer.Normalize(filter.Search);
        var parts = new List<string?> { filter.Type, filter.ChildCode, filter.Case, filter.Tense, filter.Voice };

        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            parts.Add(normalizedSearch);
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
        var normalized = string.Join('|', parts.Select(part => string.IsNullOrWhiteSpace(part) ? "_" : part.Trim()));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }

    private static string TableViewKey(WordTypeTableView tableView) => tableView switch
    {
        WordTypeTableView.Words => WordTypeTableViewKeys.Words,
        WordTypeTableView.Roots => WordTypeTableViewKeys.Roots,
        WordTypeTableView.Stems => WordTypeTableViewKeys.Stems,
        WordTypeTableView.Lemmas => WordTypeTableViewKeys.Lemmas,
        _ => tableView.ToString(),
    };

    private static string SortKey(WordTypeSort sort) => sort switch
    {
        WordTypeSort.Occurrences => WordTypeSortKeys.Occurrences,
        WordTypeSort.Ayahs => WordTypeSortKeys.Ayahs,
        WordTypeSort.Surahs => WordTypeSortKeys.Surahs,
        WordTypeSort.MushafOrder => WordTypeSortKeys.MushafOrder,
        WordTypeSort.Alpha => WordTypeSortKeys.Alpha,
        _ => sort.ToString(),
    };
}
