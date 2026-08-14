using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Commands.ApplyLinkingWorkspaceSourceDelta;

public sealed class ApplyLinkingWorkspaceSourceDeltaHandler(ILinkingWorkspaceWriter writer)
{
    public async Task<ApplyLinkingWorkspaceSourceDeltaOutcome> HandleAsync(
        ApplyLinkingWorkspaceSourceDeltaCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Delta.Changes.Count == 0)
        {
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.InvalidRequest(
                new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                    "changes",
                    null));
        }

        try
        {
            var acknowledgement = await writer.ApplyDeltaAsync(
                command.UserId,
                command.SourceId,
                command.Delta,
                cancellationToken);
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.Success(acknowledgement);
        }
        catch (LinkingWorkspaceViolationException exception)
        {
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.InvalidRequest(exception.Violation);
        }
        catch (LinkingWorkspaceSourceNotFoundException)
        {
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.SourceNotFound();
        }
        catch (LinkingStaleVersionException)
        {
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.StaleVersion();
        }
        catch (LinkingDataStaleException)
        {
            return new ApplyLinkingWorkspaceSourceDeltaOutcome.LinkingDataStale();
        }
    }
}
