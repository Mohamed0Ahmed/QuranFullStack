using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ReplaceLinkingWorkspaceSourceConfiguration;

public sealed record ReplaceLinkingWorkspaceSourceConfigurationCommand(
    int UserId,
    long SourceId,
    LinkingWorkspaceConfigurationInput Configuration,
    uint SourceVersion);
