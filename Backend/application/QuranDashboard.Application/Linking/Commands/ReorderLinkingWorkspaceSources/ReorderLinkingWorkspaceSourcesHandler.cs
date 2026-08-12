using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ReorderLinkingWorkspaceSources;

public sealed class ReorderLinkingWorkspaceSourcesHandler(
    ILogger<ReorderLinkingWorkspaceSourcesHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "ReorderLinkingWorkspaceSources";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        ReorderLinkingWorkspaceSourcesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.ReorderSourcesAsync(
                command.UserId, command.SourceIds, command.WorkspaceVersion, cancellationToken));
    }
}
