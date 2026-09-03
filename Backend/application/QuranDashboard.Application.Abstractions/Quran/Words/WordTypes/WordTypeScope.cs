namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public sealed class WordTypeScope
{
    private const string DefaultType = "noun";

    private static readonly HashSet<string> AllowedTypes = ["noun", "verb", "particle", "inl"];
    private static readonly HashSet<string> NounChildCodes =
        ["N", "PN", "ADJ", "PRON", "REL", "DEM", "T", "LOC", "TIM", "IMPN"];
    private static readonly HashSet<string> VerbChildCodes = ["past", "present", "imperative"];
    private static readonly HashSet<string> ParticleChildCodes =
    [
        "P", "CONJ", "NEG", "VOC", "IMPV", "ACC", "EMPH", "REM", "ANS", "PRO", "FUT", "INTG", "COND", "PREV", "CAUS", "AMD", "EXL",
        "RES", "PRP", "COM", "DET", "SUB", "AVR", "CERT", "CIRC", "EQ", "EXH", "EXP", "INC", "INT", "RET", "RSLT", "SUP", "SUR",
    ];
    private static readonly HashSet<string> AllowedCases = ["all", "nominative", "accusative", "genitive", "null"];
    private static readonly HashSet<string> AllowedTenses = ["all", "past", "present", "imperative"];
    private static readonly HashSet<string> AllowedVoices = ["all", "active", "passive"];

    private WordTypeScope(string type, string? childCode, string? @case, string? tense, string? voice)
    {
        Type = type;
        ChildCode = childCode;
        Case = @case;
        Tense = tense;
        Voice = voice;
    }

    public string Type { get; }
    public string? ChildCode { get; }
    public string? Case { get; }
    public string? Tense { get; }
    public string? Voice { get; }

    public static WordTypeScope? Create(
        string? type,
        string? childCode,
        string? @case,
        string? tense,
        string? voice)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? DefaultType : type.Trim().ToLowerInvariant();
        var normalizedChildCode = NormalizeOptional(childCode);
        var normalizedCase = NormalizeOptional(@case);
        var normalizedTense = NormalizeOptional(tense);
        var normalizedVoice = NormalizeOptional(voice);

        if (!AllowedTypes.Contains(normalizedType)
            || !IsValidChildCode(normalizedType, normalizedChildCode)
            || !IsValidSecondaryFilters(normalizedType, normalizedCase, normalizedTense, normalizedVoice))
        {
            return null;
        }

        return new WordTypeScope(normalizedType, normalizedChildCode, normalizedCase, normalizedTense, normalizedVoice);
    }

    private static bool IsValidChildCode(string type, string? childCode) =>
        childCode is null
        || (type == "noun" && NounChildCodes.Contains(childCode))
        || (type == "verb" && VerbChildCodes.Contains(childCode))
        || (type == "particle" && ParticleChildCodes.Contains(childCode));

    private static bool IsValidSecondaryFilters(string type, string? @case, string? tense, string? voice) =>
        (!HasConcreteValue(@case) || (type == "noun" && AllowedCases.Contains(@case!)))
        && (!HasConcreteValue(tense) || (type == "verb" && AllowedTenses.Contains(tense!)))
        && (!HasConcreteValue(voice) || (type == "verb" && AllowedVoices.Contains(voice!)));

    internal static bool AreValidIdentitySecondaryValues(string? @case, string? tense, string? voice) =>
        (!HasConcreteRawValue(@case) || AllowedCases.Contains(@case!))
        && (!HasConcreteRawValue(tense) || AllowedTenses.Contains(tense!))
        && (!HasConcreteRawValue(voice) || AllowedVoices.Contains(voice!));

    internal static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasConcreteValue(string? value) => value is not null && value != "all";

    private static bool HasConcreteRawValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value != "all";
}
