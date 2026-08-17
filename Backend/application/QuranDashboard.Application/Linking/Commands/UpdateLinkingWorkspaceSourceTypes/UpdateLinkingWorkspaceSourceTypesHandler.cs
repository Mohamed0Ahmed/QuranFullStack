using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.UpdateLinkingWorkspaceSourceTypes;

public sealed class UpdateLinkingWorkspaceSourceTypesHandler(
    ILogger<UpdateLinkingWorkspaceSourceTypesHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "UpdateLinkingWorkspaceSourceTypes";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        UpdateLinkingWorkspaceSourceTypesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (LinkingSourceDescriptorValidation.TypeCodesError(command.TypeCodes) is not null)
        {
            return Task.FromResult<LinkingWorkspaceOutcome>(new LinkingWorkspaceOutcome.InvalidRequest(
                new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                    "typeCodes",
                    null)));
        }

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.UpdateSourceTypesAsync(
                command.UserId,
                command.SourceId,
                command.TypeCodes,
                command.WorkspaceVersion,
                command.SourceVersion,
                cancellationToken));
    }
}
