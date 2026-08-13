using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public static class LinkingContributionIdentity
{
    public static string For(
        LinkingSourceDescriptor descriptor,
        LinkingContributionMode contributionMode) =>
        $"{LinkingSourceIdentity.For(descriptor)}|link-mode:{LinkingOperationTokens.ToToken(contributionMode)}";
}
