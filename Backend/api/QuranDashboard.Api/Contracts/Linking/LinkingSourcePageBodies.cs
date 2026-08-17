using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingSourcePageBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }
    public long? ExpectedLinkingDataRevision { get; init; }
    public string? ExpectedSourceViewIdentity { get; init; }
    public LinkingSourcePageViewBody? View { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed record LinkingSourcePageViewBody
{
    public string? Segment { get; init; }
    public string? InclusionMode { get; init; }
    public IReadOnlyList<int>? AyahOverrideIds { get; init; }
    public IReadOnlyList<string>? TypeCodes { get; init; }
}

internal static class LinkingSourcePageBodyMapper
{
    internal static bool TryMapView(
        LinkingSourcePageViewBody? body,
        out LinkingSourcePageView view)
    {
        view = null!;
        if (body?.Segment is null)
        {
            return false;
        }

        var segment = body.Segment switch
        {
            "all" => LinkingSourcePageSegment.All,
            "included" => LinkingSourcePageSegment.Included,
            "excluded" => LinkingSourcePageSegment.Excluded,
            _ => (LinkingSourcePageSegment?)null,
        };
        if (segment is null)
        {
            return false;
        }

        Domain.Linking.LinkingInclusionMode? inclusionMode = null;
        if (body.InclusionMode is not null)
        {
            if (!LinkingWorkspaceTokens.TryParseInclusionMode(body.InclusionMode, out var parsed))
            {
                return false;
            }

            inclusionMode = parsed;
        }

        view = new LinkingSourcePageView(
            segment.Value,
            inclusionMode,
            [.. body.AyahOverrideIds ?? []],
            [.. body.TypeCodes ?? []]);
        return true;
    }
}
