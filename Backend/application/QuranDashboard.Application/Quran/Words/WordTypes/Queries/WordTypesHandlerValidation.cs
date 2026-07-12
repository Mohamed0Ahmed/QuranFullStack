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
    private const string ParticleType = "particle";

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];

    // Catalogue-defined noun head-POS child codes (research R4 / data-model §2). These mirror
    // quran_pos_tags noun-category rows.
    private static readonly HashSet<string> NounChildCodes = new(StringComparer.Ordinal)
    {
        "N", "PN", "ADJ", "PRON", "REL", "DEM", "T", "LOC", "TIM", "IMPN",
    };

    private static readonly HashSet<string> VerbChildCodes = new(StringComparer.Ordinal)
    {
        "past", "present", "imperative",
    };

    // Particle child codes mirror quran_pos_tags.category = 'particle' except INL, which is its
    // own main type. Validation accepts catalogue particle children even when the tree hides them
    // because the count is zero, so deep links to hidden children still resolve cleanly.
    private static readonly HashSet<string> ParticleChildCodes = new(StringComparer.Ordinal)
    {
        "P", "CONJ", "NEG", "VOC", "IMPV", "ACC", "EMPH", "REM", "ANS", "PRO", "FUT", "INTG", "COND", "PREV", "CAUS", "AMD", "EXL",
        "RES", "PRP", "COM", "DET", "SUB", "AVR", "CERT", "CIRC", "EQ", "EXH", "EXP", "INC", "INT", "RET", "RSLT", "SUP", "SUR",
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

    // A child code is valid only when it belongs to the selected parent. particle now accepts
    // catalogue child codes; inl remains a leaf with no child nodes.
    public static bool IsValidChildCode(string? type, string? childCode) =>
        string.IsNullOrWhiteSpace(childCode)
        || (type == NounType && NounChildCodes.Contains(childCode))
        || (type == VerbType && VerbChildCodes.Contains(childCode))
        || (type == ParticleType && ParticleChildCodes.Contains(childCode));

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

    // Shared filter construction so every grouped-detail handler builds the identical WordTypeFilter
    // (normalized type, trimmed/null-collapsed child and secondary values) before calling IsValidFilter.
    public static WordTypeFilter NormalizeFilter(string? type, string? childCode, string? @case, string? tense, string? voice) =>
        new(
            NormalizeType(type),
            NormalizeOptional(childCode),
            NormalizeOptional(@case),
            NormalizeOptional(tense),
            NormalizeOptional(voice));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValidPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxPageSize;
}
