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

    // Secondary filter value sets. "all" is the frontend default meaning "no filter applied" and is
    // always valid wherever that filter family is permitted. "null" is a real noun-case value
    // meaning غير محدد (NULL case_feature in the source data).
    private static readonly HashSet<string> AllowedCases =
        ["all", "nominative", "accusative", "genitive", "null"];
    private static readonly HashSet<string> AllowedTenses = ["all", "past", "present", "imperative"];
    private static readonly HashSet<string> AllowedVoices = ["all", "active", "passive"];

    public static bool IsValidFilter(WordTypeFilter filter) =>
        filter.Type is null
        || (AllowedTypes.Contains(filter.Type)
            && IsValidChildCode(filter.Type, filter.ChildCode)
            && IsValidSecondaryFilter(filter.Type, filter.Case, filter.Tense, filter.Voice));

    // A child code is valid only when it belongs to the selected parent. particle and inl are
    // parents/leaves with no child nodes in v1, so any child code on them is rejected here.
    public static bool IsValidChildCode(string? type, string? childCode) =>
        string.IsNullOrWhiteSpace(childCode)
        || (type == NounType && NounChildCodes.Contains(childCode))
        || (type == VerbType && VerbChildCodes.Contains(childCode));

    // Secondary filter visibility: case applies only to noun; tense/voice apply only to verb;
    // particle and inl reject every secondary filter. "all" is the no-op default the frontend always
    // sends (even for particle/inl), so it is treated as the absence of that filter. Concrete values
    // are also validated against their value sets so an unknown code maps to InvalidFilter.
    public static bool IsValidSecondaryFilter(string? type, string? @case, string? tense, string? voice)
    {
        var hasCase = HasConcreteFilter(@case);
        var hasTense = HasConcreteFilter(tense);
        var hasVoice = HasConcreteFilter(voice);

        if (hasCase && (type != NounType || !AllowedCases.Contains(@case!)))
        {
            return false;
        }

        if (hasTense && (type != VerbType || !AllowedTenses.Contains(tense!)))
        {
            return false;
        }

        if (hasVoice && (type != VerbType || !AllowedVoices.Contains(voice!)))
        {
            return false;
        }

        return true;
    }

    // "all" is the frontend's explicit no-filter sentinel; everything else is a concrete filter that
    // must be validated against its value set.
    private static bool HasConcreteFilter(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value != "all";

    // Selected-row reads carry the active secondary values that participated in the row identity but
    // not the main type, so only the value sets are validated here; cross-type visibility is enforced
    // by the rows handler and the frontend URL normalization. An unknown concrete value maps to a
    // controlled InvalidIdentity outcome rather than silently matching nothing.
    public static bool IsValidIdentitySecondaryValues(string? @case, string? tense, string? voice) =>
        (!HasConcreteFilter(@case) || AllowedCases.Contains(@case!))
        && (!HasConcreteFilter(tense) || AllowedTenses.Contains(tense!))
        && (!HasConcreteFilter(voice) || AllowedVoices.Contains(voice!));

    public static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim().ToLowerInvariant();

    public static bool IsValidPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxPageSize;
}
