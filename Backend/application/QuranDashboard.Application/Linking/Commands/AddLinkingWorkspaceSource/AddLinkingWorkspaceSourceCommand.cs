using QuranDashboard.Domain.Linking;
using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.AddLinkingWorkspaceSource;

public sealed record AddLinkingWorkspaceSourceCommand(
    int UserId,
    LinkingSourceDescriptor Descriptor,
    LinkingSourceConfiguration? InitialConfiguration,
    uint? WorkspaceVersion);
