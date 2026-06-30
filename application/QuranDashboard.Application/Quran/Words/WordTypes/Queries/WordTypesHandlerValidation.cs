using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries;

internal static class WordTypesHandlerValidation
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
    public const string DefaultType = "noun";
    public const string DefaultSort = WordTypeSortKeys.Occurrences;

    private const string NounType = "noun";
    private const string VerbType = "verb";

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];

    // Catalogue-defined noun head-POS child codes (research R4 / data-model §2). These mirror
    // quran_pos_tags noun-category rows; particle/INL expose no child nodes in v1.
    private static readonly HashSet<string> NounChildCodes = new(StringComparer.Ordinal)
    {
        "N", "PN", "ADJ", "PRON", "REL", "DEM", "T", "LOC", "TIM", "IMPN",
    };

    private static readonly HashSet<string> VerbChildCodes = new(StringComparer.Ordinal)
    {
        "past", "present", "imperative",
    };

    public static bool IsValidFilter(WordTypeFilter filter) =>
        filter.Type is null
        || (AllowedTypes.Contains(filter.Type) && IsValidChildCode(filter.Type, filter.ChildCode));

    // A child code is valid only when it belongs to the selected parent. particle and inl are
    // parents/leaves with no child nodes in v1, so any child code on them is rejected here.
    public static bool IsValidChildCode(string? type, string? childCode) =>
        string.IsNullOrWhiteSpace(childCode)
        || (type == NounType && NounChildCodes.Contains(childCode))
        || (type == VerbType && VerbChildCodes.Contains(childCode));

    public static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim().ToLowerInvariant();

    public static bool IsValidPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxPageSize;
}
