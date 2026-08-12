namespace QuranDashboard.Application.Linking.Commands.ReorderLinkingWorkspaceSources;

public sealed record ReorderLinkingWorkspaceSourcesCommand(
    int UserId,
    IReadOnlyList<long> SourceIds,
    uint WorkspaceVersion);
