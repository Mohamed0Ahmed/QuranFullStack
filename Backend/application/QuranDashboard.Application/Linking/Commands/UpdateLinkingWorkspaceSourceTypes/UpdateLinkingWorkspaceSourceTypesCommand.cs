namespace QuranDashboard.Application.Linking.Commands.UpdateLinkingWorkspaceSourceTypes;

public sealed record UpdateLinkingWorkspaceSourceTypesCommand(
    int UserId,
    long SourceId,
    IReadOnlyList<string> TypeCodes,
    uint WorkspaceVersion,
    uint SourceVersion);
