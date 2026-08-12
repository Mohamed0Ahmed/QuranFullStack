using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingOperationTokens
{
    private static readonly Dictionary<LinkingContributionMode, string> ContributionModeToTokenMap = new()
    {
        [LinkingContributionMode.Automatic] = "automatic",
        [LinkingContributionMode.ManualSingle] = "manual_single",
        [LinkingContributionMode.ManualIndependent] = "manual_independent",
        [LinkingContributionMode.ManualGrouped] = "manual_grouped",
    };

    private static readonly Dictionary<string, LinkingContributionMode> TokenToContributionModeMap =
        ContributionModeToTokenMap.ToDictionary(
            entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);

    public static IReadOnlyList<string> ContributionModeTokens { get; } = [.. ContributionModeToTokenMap.Values];

    public static IReadOnlyList<string> ManualContributionModeTokens { get; } =
    [
        .. ContributionModeToTokenMap
            .Where(entry => entry.Key != LinkingContributionMode.Automatic)
            .Select(entry => entry.Value)
    ];

    public static string ToToken(LinkingContributionMode mode) =>
        ContributionModeToTokenMap.TryGetValue(mode, out var token)
            ? token
            : throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown linking contribution mode.");

    public static bool TryParseContributionMode(string? token, out LinkingContributionMode mode)
    {
        if (token is not null && TokenToContributionModeMap.TryGetValue(token, out var parsed))
        {
            mode = parsed;
            return true;
        }

        mode = default;
        return false;
    }

    public static bool IsManual(LinkingContributionMode mode) => mode != LinkingContributionMode.Automatic;
}
