using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.DeleteDoor;

public sealed class DeleteDoorHandler(
    ILogger<DeleteDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "DeleteDoor";

    public async Task<DeleteDoorOutcome> HandleAsync(DeleteDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var deleted = await writer.DeleteAsync(command.Id, command.Version, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
                return new DeleteDoorOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
            return new DeleteDoorOutcome.Success();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new DeleteDoorOutcome.StaleVersion();
        }
    }
}
