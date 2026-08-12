using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingSourceTokens
{
    private static readonly Dictionary<LinkingSourceKind, string> KindToTokenMap = new()
    {
        [LinkingSourceKind.UniqueWord] = "unique-word",
        [LinkingSourceKind.Root] = "root",
        [LinkingSourceKind.Lemma] = "lemma",
        [LinkingSourceKind.Stem] = "stem",
        [LinkingSourceKind.WordType] = "word-type",
        [LinkingSourceKind.ManualMushafAyahs] = "manual-mushaf-ayahs",
    };

    private static readonly Dictionary<LinkingUniqueWordMode, string> ModeToTokenMap = new()
    {
        [LinkingUniqueWordMode.Simple] = "simple",
        [LinkingUniqueWordMode.Tashkeel] = "tashkeel",
    };

    private static readonly Dictionary<LinkingWordTypeSelectionKind, string> SelectionToTokenMap = new()
    {
        [LinkingWordTypeSelectionKind.Word] = "word",
        [LinkingWordTypeSelectionKind.Root] = "root",
        [LinkingWordTypeSelectionKind.Stem] = "stem",
        [LinkingWordTypeSelectionKind.Lemma] = "lemma",
    };

    private static readonly Dictionary<string, LinkingSourceKind> TokenToKindMap = Invert(KindToTokenMap);

    private static readonly Dictionary<string, LinkingUniqueWordMode> TokenToModeMap = Invert(ModeToTokenMap);

    private static readonly Dictionary<string, LinkingWordTypeSelectionKind> TokenToSelectionMap =
        Invert(SelectionToTokenMap);

    public static IReadOnlyList<string> KindTokens { get; } = [.. KindToTokenMap.Values];

    public static IReadOnlyList<string> UniqueWordModeTokens { get; } = [.. ModeToTokenMap.Values];

    public static IReadOnlyList<string> WordTypeSelectionTokens { get; } = [.. SelectionToTokenMap.Values];

    public static string ToToken(LinkingSourceKind kind) =>
        KindToTokenMap.TryGetValue(kind, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown linking source kind.");

    public static string ToToken(LinkingUniqueWordMode mode) =>
        ModeToTokenMap.TryGetValue(mode, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown unique word mode.");

    public static string ToToken(LinkingWordTypeSelectionKind kind) =>
        SelectionToTokenMap.TryGetValue(kind, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown word type selection kind.");

    public static bool TryParseKind(string? token, out LinkingSourceKind kind) =>
        TryParse(TokenToKindMap, token, out kind);

    public static bool TryParseUniqueWordMode(string? token, out LinkingUniqueWordMode mode) =>
        TryParse(TokenToModeMap, token, out mode);

    public static bool TryParseWordTypeSelection(string? token, out LinkingWordTypeSelectionKind kind) =>
        TryParse(TokenToSelectionMap, token, out kind);

    private static Dictionary<string, TEnum> Invert<TEnum>(Dictionary<TEnum, string> map)
        where TEnum : struct, Enum =>
        map.ToDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

    private static bool TryParse<TEnum>(Dictionary<string, TEnum> map, string? token, out TEnum value)
        where TEnum : struct, Enum
    {
        if (token is not null && map.TryGetValue(token, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = default;
        return false;
    }
}
