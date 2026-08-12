using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingWorkspaceConfigurationInput(
    string Label,
    LinkingInclusionMode InclusionMode,
    IReadOnlyList<int> AyahOverrides,
    IReadOnlyList<LinkingWorkspaceSelectedWordInput> SelectedWords,
    bool? AutomaticWordMatchesEnabled,
    LinkingManualLinkShape? ManualLinkShape,
    IReadOnlyList<LinkingWorkspaceDescriptionInput> Descriptions);

public sealed record LinkingWorkspaceSelectedWordInput(int AyahId, int QuranWordId);

public sealed record LinkingWorkspaceDescriptionInput(int AyahId, int OrderValue, string Body);
