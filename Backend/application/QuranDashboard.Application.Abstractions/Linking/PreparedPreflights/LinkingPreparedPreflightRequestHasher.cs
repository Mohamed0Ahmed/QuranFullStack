using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public static class LinkingPreparedPreflightRequestHasher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ComputeCanonicalDocument(CreateLinkingPreparedPreflightRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var canonical = new
        {
            schemaVersion = 1,
            request.DoorId,
            request.ExpectedLinkingDataRevision,
            sources = request.Sources
                .OrderBy(source => source.OrderValue)
                .Select(source => new
                {
                    source.OrderValue,
                    workspaceSource = source.WorkspaceSource,
                    inlineSource = source.InlineSource is null
                        ? null
                        : new
                        {
                            resolutionIdentity = LinkingSourceIdentity.For(
                                source.InlineSource.Descriptor),
                            contributionIdentity = LinkingContributionIdentity.For(
                                source.InlineSource.Descriptor,
                                ContributionModeOf(source.InlineSource)),
                            source.InlineSource.Configuration.InclusionMode,
                            ayahOverrideIds = source.InlineSource.Configuration.AyahOverrides
                                .Distinct()
                                .Order(),
                            selectedWords = source.InlineSource.Configuration.SelectedWords
                                .Distinct()
                                .OrderBy(word => word.AyahId)
                                .ThenBy(word => word.QuranWordId),
                            source.InlineSource.Configuration.AutomaticWordMatchesEnabled,
                            source.InlineSource.Configuration.ManualLinkShape,
                            descriptions = source.InlineSource.Configuration.Descriptions
                                .OrderBy(description => description.AyahId)
                                .ThenBy(description => description.OrderValue),
                        },
                }),
        };

        return JsonSerializer.Serialize(canonical, JsonOptions);
    }

    public static string ComputeHash(string canonicalDocument)
    {
        ArgumentNullException.ThrowIfNull(canonicalDocument);
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonicalDocument)));
    }

    public static LinkingContributionMode ContributionModeOf(LinkingPreparedInlineSource source)
    {
        if (source.Descriptor.Kind != Domain.Linking.LinkingSourceKind.ManualMushafAyahs)
        {
            return LinkingContributionMode.Automatic;
        }

        return source.Configuration.ManualLinkShape == Domain.Linking.LinkingManualLinkShape.Grouped
            ? LinkingContributionMode.ManualGrouped
            : LinkingContributionMode.ManualIndependent;
    }
}
