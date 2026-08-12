using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public sealed record LinkingWorkspaceConfigurationInput(
    string Label,
    LinkingInclusionMode InclusionMode,
    IReadOnlyList<int> AyahOverrides,
    IReadOnlyList<LinkingWorkspaceSelectedWordInput> SelectedWords,
    bool? AutomaticWordMatchesEnabled,
    LinkingManualLinkShape? ManualLinkShape);

public sealed record LinkingWorkspaceSelectedWordInput(int AyahId, int QuranWordId);
