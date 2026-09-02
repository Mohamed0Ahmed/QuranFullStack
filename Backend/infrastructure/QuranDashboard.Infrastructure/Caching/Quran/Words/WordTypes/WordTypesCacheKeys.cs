using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

public static class WordTypesCacheKeys
{
    public const string Tree = "wordtypes:tree";

    public static string Rows(WordTypeFilter filter, WordTypeSortSpec sort, WordTypeListPaging paging) =>
        $"wordtypes:rows:{HashFilter(filter)}:sort:{SortKey(sort)}:p{paging.Page}:s{paging.PageSize}";

    public static string Table(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSortSpec sort, WordTypeListPaging paging) =>
        $"wordtypes:table:{HashFilter(filter)}:view:{tableView.Key}:sort:{SortKey(sort)}:p{paging.Page}:s{paging.PageSize}";

    // Scoped four-count summary (Feature 026, US8). The key folds in EVERY scope input via HashFilter
    // (type, childCode, case, tense, voice, normalized search, presence flags) and NOTHING else — no
    // tableView, no sort, no page — so counts are shared across tab/page changes but isolated per scope.
    public static string ScopeCounts(WordTypeFilter filter) =>
        $"wordtypes:scope-counts:{HashFilter(filter)}";

    public static string Summary(WordTypeRowIdentity identity) =>
        $"wordtypes:summary:{HashIdentity(identity)}";

    public static string Ayahs(WordTypeRowIdentity identity, WordTypeDetailPaging paging) =>
        $"wordtypes:ayahs:{HashIdentity(identity)}:p{paging.Page}:s{paging.PageSize}";

    public static string Surahs(WordTypeRowIdentity identity) =>
        $"wordtypes:surahs:{HashIdentity(identity)}";

    // Grouped detail keys expose only the kind/view labels in the readable prefix; the dimension ID and
    // the full five-field scope are folded into the hash so different scopes never cross-serve. Each view
    // carries its own segment (summary vs words vs …) so views never share a prefix; paged views append
    // page/pageSize.
    public static string GroupedSummary(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.RouteKey}:summary:{HashGroupedSelection(selection)}";

    public static string GroupedWords(WordTypeGroupedSelection selection, WordTypeDetailPaging paging) =>
        $"wordtypes:grouped:{selection.Kind.RouteKey}:words:{HashGroupedSelection(selection)}:p{paging.Page}:s{paging.PageSize}";

    public static string GroupedAyahs(WordTypeGroupedSelection selection, WordTypeDetailPaging paging) =>
        $"wordtypes:grouped:{selection.Kind.RouteKey}:ayahs:{HashGroupedSelection(selection)}:p{paging.Page}:s{paging.PageSize}";

    // Surahs are single-shot, so the key carries no page component (mirrors the summary key shape).
    public static string GroupedSurahs(WordTypeGroupedSelection selection) =>
        $"wordtypes:grouped:{selection.Kind.RouteKey}:surahs:{HashGroupedSelection(selection)}";

    private static string HashGroupedSelection(WordTypeGroupedSelection selection) => HashParts(
        selection.DimensionId.ToString(CultureInfo.InvariantCulture),
        selection.Scope.Type,
        selection.Scope.ChildCode,
        selection.Scope.Case,
        selection.Scope.Tense,
        selection.Scope.Voice);

    // Empty/absent search AND absent presence flags keep the pre-feature 5-part hash so warm rows/table
    // entries stay valid. A non-empty NORMALIZED search appends a LABELLED component (same normalization
    // the SQL predicate uses); any set presence flag (Feature 026, US6) appends a distinct LABELLED flag
    // component. The distinct "search:"/"flags:" prefixes plus the delimiter escaping in HashParts stop a
    // free-form search term (e.g. one that normalizes to "flags:1__" or embeds the '|' delimiter) from
    // hashing to the same key as a different presence-flag scope — so searched/flagged reads never
    // cross-serve their unsearched/unflagged counterparts.
    private static string HashFilter(WordTypeFilter filter)
    {
        var normalizedSearch = ArabicSearchQueryNormalizer.Normalize(filter.Search);
        var parts = new List<string?>
        {
            filter.Scope.Type,
            filter.Scope.ChildCode,
            filter.Scope.Case,
            filter.Scope.Tense,
            filter.Scope.Voice,
        };

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

    internal static string HashParts(params string?[] parts)
    {
        var normalized = string.Join('|', parts.Select(EncodePart));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }

    // Escape the join delimiter (and its own escape char) so a part that contains '|' cannot split into
    // extra slots and align with a different combination of parts. Absent/whitespace parts collapse to
    // the reserved "_" placeholder, matching the pre-feature key shape for unfiltered reads.
    private static string EncodePart(string? part) =>
        string.IsNullOrWhiteSpace(part) ? "_" : part.Trim().Replace("\\", "\\\\").Replace("|", "\\|");

    // The CANONICAL token, so alias and canonical spellings of one ordering share ONE entry
    // ("occurrences-desc" keys as "occurrences") and every pre-feature key stays byte-identical.
    // Deliberately not a ToString() fallback: an unmapped value must fail loudly rather than silently
    // fork the cache under a second key for the same rows.
    private static string SortKey(WordTypeSortSpec sort) => sort.CanonicalToken();
}
