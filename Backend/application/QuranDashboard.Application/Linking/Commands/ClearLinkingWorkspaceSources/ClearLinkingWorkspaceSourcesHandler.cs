using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ClearLinkingWorkspaceSources;

public sealed class ClearLinkingWorkspaceSourcesHandler(
    ILogger<ClearLinkingWorkspaceSourcesHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "ClearLinkingWorkspaceSources";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        ClearLinkingWorkspaceSourcesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.ClearSourcesAsync(command.UserId, command.WorkspaceVersion, cancellationToken));
    }
}
