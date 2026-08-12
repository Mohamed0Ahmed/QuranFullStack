namespace QuranDashboard.Application.Linking.Commands.RemoveLinkingWorkspaceSource;

public sealed record RemoveLinkingWorkspaceSourceCommand(int UserId, long SourceId, uint WorkspaceVersion);
