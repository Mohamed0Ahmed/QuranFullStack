using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.RestoreDoor;

public sealed class RestoreDoorHandler(
    ILogger<RestoreDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "RestoreDoor";

    public async Task<RestoreDoorOutcome> HandleAsync(RestoreDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var door = await writer.RestoreAsync(command.Id, command.Version, cancellationToken);
            if (door is null)
            {
                logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
                return new RestoreDoorOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
            return new RestoreDoorOutcome.Success(door);
        }
        catch (AbwabParentStillArchivedException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "parentStillArchived", command.Id);
            return new RestoreDoorOutcome.ParentStillArchived();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new RestoreDoorOutcome.StaleVersion();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "duplicateName", command.Id);
            return new RestoreDoorOutcome.DuplicateName();
        }
    }
}
