using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ApplyLinkingWorkspaceSourceDelta;

public sealed record ApplyLinkingWorkspaceSourceDeltaCommand(
    int UserId,
    long SourceId,
    LinkingWorkspaceDeltaInput Delta);
