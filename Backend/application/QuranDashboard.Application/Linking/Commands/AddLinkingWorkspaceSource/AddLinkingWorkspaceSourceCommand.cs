using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Linking.Commands.AddLinkingWorkspaceSource;

public sealed record AddLinkingWorkspaceSourceCommand(
    int UserId,
    LinkingSourceDescriptor Descriptor,
    uint? WorkspaceVersion);
