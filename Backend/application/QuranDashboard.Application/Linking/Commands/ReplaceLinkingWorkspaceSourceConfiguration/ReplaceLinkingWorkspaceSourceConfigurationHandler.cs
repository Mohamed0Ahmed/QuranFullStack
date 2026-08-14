using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ReplaceLinkingWorkspaceSourceConfiguration;

public sealed class ReplaceLinkingWorkspaceSourceConfigurationHandler(
    ILogger<ReplaceLinkingWorkspaceSourceConfigurationHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "ReplaceLinkingWorkspaceSourceConfiguration";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        ReplaceLinkingWorkspaceSourceConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.ReplaceSourceConfigurationAsync(
                command.UserId,
                command.SourceId,
                command.Configuration,
                command.SourceVersion,
                command.ExpectedLinkingDataRevision,
                cancellationToken));
    }
}
