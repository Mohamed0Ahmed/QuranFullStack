using QuranDashboard.Application.Abstractions.Abwab.Inclusions;

namespace QuranDashboard.Application.Abwab.Commands.DeleteDoorInclusion;

public sealed class DeleteDoorInclusionHandler(
    ILogger<DeleteDoorInclusionHandler> logger,
    IAbwabDoorInclusionsWriter writer)
{
    public async Task<DeleteDoorInclusionOutcome> HandleAsync(
        DeleteDoorInclusionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TargetDoorId <= 0 || command.InclusionId <= 0 || command.ActorUserId <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {doorId} {inclusionId}",
                "AbwabInclusions",
                "DeleteDoorInclusion",
                "invalidRequest",
                command.TargetDoorId,
                command.InclusionId);
            return new DeleteDoorInclusionOutcome.InvalidRequest();
        }

        var result = await writer.DetachAsync(
            command.TargetDoorId,
            command.InclusionId,
            command.ExpectedTargetDoorVersion,
            command.ActorUserId,
            cancellationToken);

        DeleteDoorInclusionOutcome outcome = result switch
        {
            AbwabDoorInclusionDetachWriteResult.Success success =>
                new DeleteDoorInclusionOutcome.Success(success.Result),
            AbwabDoorInclusionDetachWriteResult.InvalidRequest =>
                new DeleteDoorInclusionOutcome.InvalidRequest(),
            AbwabDoorInclusionDetachWriteResult.NotFound =>
                new DeleteDoorInclusionOutcome.NotFound(),
            AbwabDoorInclusionDetachWriteResult.ArchivedTarget =>
                new DeleteDoorInclusionOutcome.ArchivedTarget(),
            AbwabDoorInclusionDetachWriteResult.StaleTargetVersion =>
                new DeleteDoorInclusionOutcome.StaleTargetVersion(),
            AbwabDoorInclusionDetachWriteResult.SynchronizationUnavailable =>
                new DeleteDoorInclusionOutcome.SynchronizationUnavailable(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(AbwabDoorInclusionDetachWriteResult)} variant."),
        };

        logger.LogInformation(
            "Completed {feature} {operation} {doorId} {inclusionId} {outcome}",
            "AbwabInclusions",
            "DeleteDoorInclusion",
            command.TargetDoorId,
            command.InclusionId,
            outcome.GetType().Name);

        return outcome;
    }
}
