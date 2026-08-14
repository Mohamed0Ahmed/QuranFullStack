using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

public sealed record LinkingWorkspaceDeltaBody
{
    public uint? SourceVersion { get; init; }
    public long? ExpectedLinkingDataRevision { get; init; }
    public IReadOnlyList<LinkingWorkspaceDeltaChangeBody>? Changes { get; init; }
}

public sealed record LinkingWorkspaceDeltaChangeBody
{
    public string? Kind { get; init; }
    public string? Label { get; init; }
    public int? AyahId { get; init; }
    public bool? Included { get; init; }
    public string? Mode { get; init; }
    public IReadOnlyList<int>? AyahOverrideIds { get; init; }
    public int? QuranWordId { get; init; }
    public bool? Selected { get; init; }
    public bool? Enabled { get; init; }
    public string? Shape { get; init; }
    public IReadOnlyList<string>? Descriptions { get; init; }
}

public sealed record LinkingWorkspaceDeltaResponse(
    uint WorkspaceVersion,
    long SourceId,
    uint SourceVersion,
    long LinkingDataRevision,
    IReadOnlyList<LinkingWorkspaceDeltaChangeResponse> NormalizedAppliedChanges);

public sealed record LinkingWorkspaceDeltaChangeResponse(
    string Kind,
    string? Label,
    int? AyahId,
    bool? Included,
    string? Mode,
    IReadOnlyList<int>? AyahOverrideIds,
    int? QuranWordId,
    bool? Selected,
    bool? Enabled,
    string? Shape,
    IReadOnlyList<string>? Descriptions);

internal static class LinkingWorkspaceDeltaBodyMapper
{
    internal static bool TryMap(
        LinkingWorkspaceDeltaBody? body,
        out LinkingWorkspaceDeltaInput delta)
    {
        delta = null!;
        if (body?.SourceVersion is null or 0
            || body.ExpectedLinkingDataRevision is null or <= 0
            || body.Changes is null)
        {
            return false;
        }

        var changes = new List<LinkingWorkspaceDeltaChange>(body.Changes.Count);
        foreach (var change in body.Changes)
        {
            if (!TryMapChange(change, out var mapped))
            {
                return false;
            }

            changes.Add(mapped);
        }

        delta = new LinkingWorkspaceDeltaInput(
            body.SourceVersion.Value,
            body.ExpectedLinkingDataRevision.Value,
            changes);
        return true;
    }

    internal static LinkingWorkspaceDeltaResponse ToResponse(
        LinkingWorkspaceDeltaAcknowledgement acknowledgement) =>
        new(
            acknowledgement.WorkspaceVersion,
            acknowledgement.SourceId,
            acknowledgement.SourceVersion,
            acknowledgement.LinkingDataRevision,
            [.. acknowledgement.NormalizedAppliedChanges.Select(ToResponse)]);

    private static bool TryMapChange(
        LinkingWorkspaceDeltaChangeBody? body,
        out LinkingWorkspaceDeltaChange change)
    {
        change = null!;
        if (body is null)
        {
            return false;
        }

        change = body.Kind switch
        {
            "set-label" when body.Label is not null =>
                new LinkingWorkspaceDeltaChange.SetLabel(body.Label),
            "set-ayah-included" when body.AyahId is > 0 && body.Included is not null =>
                new LinkingWorkspaceDeltaChange.SetAyahIncluded(body.AyahId.Value, body.Included.Value),
            "replace-inclusion" when TryInclusion(body.Mode, out var mode)
                && body.AyahOverrideIds is not null
                && body.AyahOverrideIds.All(id => id > 0) =>
                new LinkingWorkspaceDeltaChange.ReplaceInclusion(mode, [.. body.AyahOverrideIds]),
            "set-word-selected" when body.AyahId is > 0
                && body.QuranWordId is > 0
                && body.Selected is not null =>
                new LinkingWorkspaceDeltaChange.SetWordSelected(
                    body.AyahId.Value,
                    body.QuranWordId.Value,
                    body.Selected.Value),
            "set-automatic-word-matches" when body.Enabled is not null =>
                new LinkingWorkspaceDeltaChange.SetAutomaticWordMatches(body.Enabled.Value),
            "set-manual-link-shape" when TryShape(body.Shape, out var shape) =>
                new LinkingWorkspaceDeltaChange.SetManualLinkShape(shape),
            "replace-ayah-descriptions" when body.AyahId is > 0 && body.Descriptions is not null =>
                new LinkingWorkspaceDeltaChange.ReplaceAyahDescriptions(
                    body.AyahId.Value,
                    [.. body.Descriptions]),
            _ => null!,
        };

        return change is not null;
    }

    private static bool TryInclusion(
        string? token,
        out Domain.Linking.LinkingInclusionMode mode) =>
        LinkingWorkspaceTokens.TryParseInclusionMode(token, out mode);

    private static bool TryShape(
        string? token,
        out Domain.Linking.LinkingManualLinkShape shape) =>
        LinkingWorkspaceTokens.TryParseManualLinkShape(token, out shape);

    private static LinkingWorkspaceDeltaChangeResponse ToResponse(LinkingWorkspaceDeltaChange change) =>
        change switch
        {
            LinkingWorkspaceDeltaChange.SetLabel value => Empty(value.Kind) with { Label = value.Label },
            LinkingWorkspaceDeltaChange.SetAyahIncluded value =>
                Empty(value.Kind) with { AyahId = value.AyahId, Included = value.Included },
            LinkingWorkspaceDeltaChange.ReplaceInclusion value => Empty(value.Kind) with
            {
                Mode = LinkingWorkspaceTokens.ToToken(value.Mode),
                AyahOverrideIds = value.AyahOverrideIds,
            },
            LinkingWorkspaceDeltaChange.SetWordSelected value => Empty(value.Kind) with
            {
                AyahId = value.AyahId,
                QuranWordId = value.QuranWordId,
                Selected = value.Selected,
            },
            LinkingWorkspaceDeltaChange.SetAutomaticWordMatches value =>
                Empty(value.Kind) with { Enabled = value.Enabled },
            LinkingWorkspaceDeltaChange.SetManualLinkShape value =>
                Empty(value.Kind) with { Shape = LinkingWorkspaceTokens.ToToken(value.Shape) },
            LinkingWorkspaceDeltaChange.ReplaceAyahDescriptions value => Empty(value.Kind) with
            {
                AyahId = value.AyahId,
                Descriptions = value.Descriptions,
            },
            _ => throw new InvalidOperationException("Unknown linking workspace delta change."),
        };

    private static LinkingWorkspaceDeltaChangeResponse Empty(string kind) =>
        new(kind, null, null, null, null, null, null, null, null, null, null);
}
