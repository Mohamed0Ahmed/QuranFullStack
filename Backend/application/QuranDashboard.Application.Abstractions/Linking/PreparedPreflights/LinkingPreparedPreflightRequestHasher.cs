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
                                source.InlineSource.Configuration.ContributionMode),
                            source.InlineSource.Configuration.InclusionMode,
                            ayahOverrideIds = source.InlineSource.Configuration.AyahOverrides,
                            selectedWords = source.InlineSource.Configuration.SelectedWords,
                            source.InlineSource.Configuration.AutomaticWordMatchesEnabled,
                            source.InlineSource.Configuration.ManualLinkShape,
                            descriptions = source.InlineSource.Configuration.Descriptions,
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

}
