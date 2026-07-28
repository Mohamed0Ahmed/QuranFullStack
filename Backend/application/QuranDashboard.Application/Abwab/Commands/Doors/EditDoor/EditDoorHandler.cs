using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.EditDoor;

public sealed class EditDoorHandler(
    ILogger<EditDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "EditDoor";

    public async Task<EditDoorOutcome> HandleAsync(EditDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new EditDoorOutcome.InvalidName();
        }

        try
        {
            var door = await writer.EditAsync(
                command.Id,
                name,
                command.Description,
                command.RepresentativeAyahText,
                command.Aliases ?? [],
                command.Version,
                cancellationToken);

            if (door is null)
            {
                logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
                return new EditDoorOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
            return new EditDoorOutcome.Success(door);
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new EditDoorOutcome.StaleVersion();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new EditDoorOutcome.DuplicateName();
        }
    }
}
