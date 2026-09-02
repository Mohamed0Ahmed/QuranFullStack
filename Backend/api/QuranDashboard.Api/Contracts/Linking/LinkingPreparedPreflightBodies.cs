using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

namespace QuranDashboard.Api.Contracts.Linking;

public sealed record CreateLinkingPreparedPreflightBody
{
    public Guid? PreparationKey { get; init; }
    public int? DoorId { get; init; }
    public long? ExpectedLinkingDataRevision { get; init; }
    public IReadOnlyList<LinkingPreparedSourceBody>? Sources { get; init; }
}

public sealed record LinkingPreparedSourceBody
{
    public int? OrderValue { get; init; }
    public LinkingPreparedWorkspaceSourceBody? WorkspaceSource { get; init; }
    public LinkingPreparedInlineSourceBody? InlineSource { get; init; }
}

public sealed record LinkingPreparedWorkspaceSourceBody
{
    public long? SourceId { get; init; }
    public uint? SourceVersion { get; init; }
}

public sealed record LinkingPreparedInlineSourceBody
{
    public LinkingSourceDescriptorBody? Descriptor { get; init; }
    public LinkingPreparedConfigurationBody? Configuration { get; init; }
}

public sealed record LinkingPreparedConfigurationBody
{
    public string? InclusionMode { get; init; }
    public IReadOnlyList<int>? AyahOverrideIds { get; init; }
    public IReadOnlyList<LinkingSelectedWordBody>? SelectedWords { get; init; }
    public bool? AutomaticWordMatchesEnabled { get; init; }
    public string? ManualLinkShape { get; init; }
    public IReadOnlyList<LinkingDescriptionBody>? Descriptions { get; init; }
}

internal static class LinkingPreparedPreflightBodyMapper
{
    internal static bool TryMap(
        CreateLinkingPreparedPreflightBody? body,
        out CreateLinkingPreparedPreflightRequest request)
    {
        request = null!;
        if (body?.PreparationKey is not { } preparationKey
            || preparationKey == Guid.Empty
            || body.DoorId is not > 0
            || body.Sources is null)
        {
            return false;
        }

        var sources = new List<LinkingPreparedSourceRequest>(body.Sources.Count);
        foreach (var source in body.Sources)
        {
            if (source?.OrderValue is not > 0
                || (source.WorkspaceSource is null) == (source.InlineSource is null))
            {
                return false;
            }

            if (source.WorkspaceSource is not null)
            {
                if (source.WorkspaceSource.SourceId is not > 0
                    || source.WorkspaceSource.SourceVersion is not > 0)
                {
                    return false;
                }

                sources.Add(new LinkingPreparedSourceRequest(
                    source.OrderValue.Value,
                    new LinkingPreparedWorkspaceSourceReference(
                        source.WorkspaceSource.SourceId.Value,
                        source.WorkspaceSource.SourceVersion.Value),
                    null));
                continue;
            }

            var inline = source.InlineSource!;
            if (!LinkingSourceDescriptorBodyMapper.TryMap(
                    inline.Descriptor,
                    out var descriptor,
                    out _)
                || !LinkingWorkspaceConfigurationBodyMapper.TryMap(
                    ToWorkspaceBody(inline.Configuration),
                    descriptor.Kind,
                    out var configuration,
                    out _))
            {
                return false;
            }

            sources.Add(new LinkingPreparedSourceRequest(
                source.OrderValue.Value,
                null,
                new LinkingPreparedInlineSource(descriptor, configuration)));
        }

        request = new CreateLinkingPreparedPreflightRequest(
            preparationKey,
            body.DoorId.Value,
            body.ExpectedLinkingDataRevision,
            sources);
        return true;
    }

    private static LinkingWorkspaceConfigurationBody? ToWorkspaceBody(
        LinkingPreparedConfigurationBody? body) =>
        body is null
            ? null
            : new LinkingWorkspaceConfigurationBody
            {
                InclusionMode = body.InclusionMode,
                AyahOverrides = body.AyahOverrideIds,
                SelectedWords = body.SelectedWords,
                AutomaticWordMatchesEnabled = body.AutomaticWordMatchesEnabled,
                ManualLinkShape = body.ManualLinkShape,
                Descriptions = body.Descriptions,
            };
}
