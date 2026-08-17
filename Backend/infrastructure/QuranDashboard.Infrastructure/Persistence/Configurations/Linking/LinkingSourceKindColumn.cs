using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

internal static class LinkingSourceKindColumn
{
    private static readonly Dictionary<LinkingSourceKind, string> KindToTokenMap = new()
    {
        [LinkingSourceKind.UniqueWord] = "unique_word",
        [LinkingSourceKind.Root] = "root",
        [LinkingSourceKind.Lemma] = "lemma",
        [LinkingSourceKind.Stem] = "stem",
        [LinkingSourceKind.WordType] = "word_type",
        [LinkingSourceKind.ManualMushafAyahs] = "manual_mushaf_ayahs",
        [LinkingSourceKind.DoorInclusion] = "door_inclusion",
    };

    private static readonly Dictionary<string, LinkingSourceKind> TokenToKindMap =
        KindToTokenMap.ToDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

    public static IReadOnlyList<string> Tokens { get; } =
    [
        .. KindToTokenMap
            .Where(entry => entry.Key != LinkingSourceKind.DoorInclusion)
            .Select(entry => entry.Value)
    ];

    public static IReadOnlyList<string> ContributionTokens { get; } = [.. KindToTokenMap.Values];

    public static string Manual => ToToken(LinkingSourceKind.ManualMushafAyahs);

    public static string DoorInclusion => ToToken(LinkingSourceKind.DoorInclusion);

    public static string ToToken(LinkingSourceKind kind) =>
        KindToTokenMap.TryGetValue(kind, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown linking source kind.");

    public static LinkingSourceKind FromToken(string token) =>
        TokenToKindMap.TryGetValue(token, out var kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown linking source kind column value.");
}
