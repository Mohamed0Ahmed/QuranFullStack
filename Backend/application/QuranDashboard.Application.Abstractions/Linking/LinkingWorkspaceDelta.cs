using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingWorkspaceDeltaInput(
    uint SourceVersion,
    long ExpectedLinkingDataRevision,
    IReadOnlyList<LinkingWorkspaceDeltaChange> Changes);

public abstract record LinkingWorkspaceDeltaChange(string Kind)
{
    public sealed record SetLabel(string Label) : LinkingWorkspaceDeltaChange("set-label");
    public sealed record SetAyahIncluded(int AyahId, bool Included)
        : LinkingWorkspaceDeltaChange("set-ayah-included");
    public sealed record ReplaceInclusion(LinkingInclusionMode Mode, IReadOnlyList<int> AyahOverrideIds)
        : LinkingWorkspaceDeltaChange("replace-inclusion");
    public sealed record SetWordSelected(int AyahId, int QuranWordId, bool Selected)
        : LinkingWorkspaceDeltaChange("set-word-selected");
    public sealed record SetAutomaticWordMatches(bool Enabled)
        : LinkingWorkspaceDeltaChange("set-automatic-word-matches");
    public sealed record SetManualLinkShape(LinkingManualLinkShape Shape)
        : LinkingWorkspaceDeltaChange("set-manual-link-shape");
    public sealed record ReplaceAyahDescriptions(int AyahId, IReadOnlyList<string> Descriptions)
        : LinkingWorkspaceDeltaChange("replace-ayah-descriptions");
}

public sealed record LinkingWorkspaceDeltaAcknowledgement(
    uint WorkspaceVersion,
    long SourceId,
    uint SourceVersion,
    long LinkingDataRevision,
    IReadOnlyList<LinkingWorkspaceDeltaChange> NormalizedAppliedChanges);
