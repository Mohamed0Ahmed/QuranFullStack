using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries;

internal static class WordTypesHandlerValidation
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;

    public const int MaxListPageSize = 1000;
    public const int MaxDetailPageSize = 100;

    public const int MaxSearchLength = 64;
    public const string DefaultType = "noun";
    public const string DefaultSort = WordTypeSortKeys.Occurrences;

    private const string NounType = "noun";
    private const string VerbType = "verb";
    private const string ParticleType = "particle";

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];

    private static readonly HashSet<string> NounChildCodes = new(StringComparer.Ordinal)
    {
        "N", "PN", "ADJ", "PRON", "REL", "DEM", "T", "LOC", "TIM", "IMPN",
    };

    private static readonly HashSet<string> VerbChildCodes = new(StringComparer.Ordinal)
    {
        "past", "present", "imperative",
    };

    private static readonly HashSet<string> ParticleChildCodes = new(StringComparer.Ordinal)
    {
        "P", "CONJ", "NEG", "VOC", "IMPV", "ACC", "EMPH", "REM", "ANS", "PRO", "FUT", "INTG", "COND", "PREV", "CAUS", "AMD", "EXL",
        "RES", "PRP", "COM", "DET", "SUB", "AVR", "CERT", "CIRC", "EQ", "EXH", "EXP", "INC", "INT", "RET", "RSLT", "SUP", "SUR",
    };

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

    public static bool IsValidSearch(string? search) =>
        search is null || search.Length <= MaxSearchLength;

    public static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    public static bool IsValidChildCode(string? type, string? childCode) =>
        string.IsNullOrWhiteSpace(childCode)
        || (type == NounType && NounChildCodes.Contains(childCode))
        || (type == VerbType && VerbChildCodes.Contains(childCode))
        || (type == ParticleType && ParticleChildCodes.Contains(childCode));

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

    private static bool HasConcreteFilter(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value != "all";

    public static bool IsValidIdentitySecondaryValues(string? @case, string? tense, string? voice) =>
        (!HasConcreteFilter(@case) || AllowedCases.Contains(@case!))
        && (!HasConcreteFilter(tense) || AllowedTenses.Contains(tense!))
        && (!HasConcreteFilter(voice) || AllowedVoices.Contains(voice!));

    public static string NormalizeType(string? type) =>
        string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim().ToLowerInvariant();

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
