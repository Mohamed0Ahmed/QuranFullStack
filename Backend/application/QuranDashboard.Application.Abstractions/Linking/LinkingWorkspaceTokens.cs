using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingWorkspaceTokens
{
    private static readonly Dictionary<LinkingInclusionMode, string> InclusionModeToTokenMap = new()
    {
        [LinkingInclusionMode.AllExcept] = "all_except",
        [LinkingInclusionMode.Only] = "only",
    };

    private static readonly Dictionary<LinkingManualLinkShape, string> ManualLinkShapeToTokenMap = new()
    {
        [LinkingManualLinkShape.Grouped] = "grouped",
        [LinkingManualLinkShape.Independent] = "independent",
    };

    private static readonly Dictionary<string, LinkingInclusionMode> TokenToInclusionModeMap =
        Invert(InclusionModeToTokenMap);

    private static readonly Dictionary<string, LinkingManualLinkShape> TokenToManualLinkShapeMap =
        Invert(ManualLinkShapeToTokenMap);

    public static IReadOnlyList<string> InclusionModeTokens { get; } = [.. InclusionModeToTokenMap.Values];

    public static IReadOnlyList<string> ManualLinkShapeTokens { get; } = [.. ManualLinkShapeToTokenMap.Values];

    public static string ToToken(LinkingInclusionMode mode) =>
        InclusionModeToTokenMap.TryGetValue(mode, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown linking inclusion mode.");

    public static string ToToken(LinkingManualLinkShape shape) =>
        ManualLinkShapeToTokenMap.TryGetValue(shape, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown linking manual link shape.");

    public static bool TryParseInclusionMode(string? token, out LinkingInclusionMode mode) =>
        TryParse(TokenToInclusionModeMap, token, out mode);

    public static bool TryParseManualLinkShape(string? token, out LinkingManualLinkShape shape) =>
        TryParse(TokenToManualLinkShapeMap, token, out shape);

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
