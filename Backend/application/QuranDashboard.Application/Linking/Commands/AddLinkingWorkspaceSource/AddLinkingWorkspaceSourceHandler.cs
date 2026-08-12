using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.AddLinkingWorkspaceSource;

public sealed class AddLinkingWorkspaceSourceHandler(
    ILogger<AddLinkingWorkspaceSourceHandler> logger,
    ILinkingWorkspaceWriter writer)
{
    private const string OperationName = "AddLinkingWorkspaceSource";
    private const string DescriptorField = "descriptor";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        AddLinkingWorkspaceSourceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!LinkingSourceDescriptorValidation.TryValidate(command.Descriptor, out var error))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {userId} {kind} {diagnostic}",
                "LinkingWorkspace",
                OperationName,
                "invalidDescriptor",
                command.UserId,
                command.Descriptor.Kind,
                error);

            return Task.FromResult<LinkingWorkspaceOutcome>(new LinkingWorkspaceOutcome.InvalidDescriptor(
                new LinkingDescriptorViolation(
                    LinkingDescriptorViolationCode.MalformedDescriptor, DescriptorField, null)));
        }

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            command.UserId,
            () => writer.AddSourceAsync(
                command.UserId, command.Descriptor, command.WorkspaceVersion, cancellationToken));
    }
}
