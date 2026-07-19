using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries;

internal static class WordTypesHandlerValidation
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    // Split cap: list reads (words/table) serve up to 1000 rows; every detail read (word ayahs, grouped
    // member words, grouped ayahs) keeps the 1..100 contract.
    public const int MaxListPageSize = 1000;
    public const int MaxDetailPageSize = 100;

    // Defensive bound on the raw (trimmed) search term; longer input is rejected as InvalidFilter.
    public const int MaxSearchLength = 64;
    public const string DefaultType = "noun";
    public const string DefaultSort = WordTypeSortKeys.Occurrences;

    private const string NounType = "noun";
    private const string VerbType = "verb";
    private const string ParticleType = "particle";

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];

    // Noun head-POS child codes; these mirror the quran_pos_tags noun-category rows.
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
        IsValidSearch(filter.Search)
        && (filter.Type is null
            || (AllowedTypes.Contains(filter.Type)
                && IsValidChildCode(filter.Type, filter.ChildCode)
                && IsValidSecondaryFilter(filter.Type, filter.Case, filter.Tense, filter.Voice)));

    // Only the length is bounded here (the value is a free word-identity fragment normalized in
    // Infrastructure); over-length input maps to InvalidFilter.
    public static bool IsValidSearch(string? search) =>
        search is null || search.Length <= MaxSearchLength;

    // Over-length is deferred to IsValidFilter (controlled 400) rather than truncated here; the value is
    // never logged (handlers log only a hasSearch boolean).
    public static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    // inl is a leaf (no child nodes), so it is intentionally absent from this parent/child check.
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

    // "all" is the frontend's no-filter sentinel, not a concrete filter.
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

    // Shared so every grouped-detail handler builds the identical WordTypeFilter before IsValidFilter.
    public static WordTypeFilter NormalizeFilter(string? type, string? childCode, string? @case, string? tense, string? voice) =>
        new(
            NormalizeType(type),
            NormalizeOptional(childCode),
            NormalizeOptional(@case),
            NormalizeOptional(tense),
            NormalizeOptional(voice));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValidListPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxListPageSize;

    public static bool IsValidDetailPaging(int page, int pageSize) =>
        page >= MinPage && pageSize >= MinPageSize && pageSize <= MaxDetailPageSize;
}
