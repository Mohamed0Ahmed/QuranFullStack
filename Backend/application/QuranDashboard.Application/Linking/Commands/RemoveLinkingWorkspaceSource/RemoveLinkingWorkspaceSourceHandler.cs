using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.RemoveLinkingWorkspaceSource;

public sealed class RemoveLinkingWorkspaceSourceHandler(
    ILogger<RemoveLinkingWorkspaceSourceHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "RemoveLinkingWorkspaceSource";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        RemoveLinkingWorkspaceSourceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.RemoveSourceAsync(
                command.UserId, command.SourceId, command.WorkspaceVersion, cancellationToken));
    }
}
