using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.CreateDoor;

public sealed class CreateDoorHandler(
    ILogger<CreateDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "CreateDoor";

    public async Task<CreateDoorOutcome> HandleAsync(CreateDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new CreateDoorOutcome.InvalidName();
        }

        try
        {
            var door = await writer.CreateAsync(
                command.SectionId,
                command.ParentId,
                name,
                command.Description,
                command.RepresentativeAyahText,
                command.Aliases ?? [],
                cancellationToken);

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, door.Id);
            return new CreateDoorOutcome.Success(door);
        }
        catch (AbwabParentNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "parentNotFound");
            return new CreateDoorOutcome.ParentNotFound();
        }
        catch (AbwabSectionNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "sectionNotFound");
            return new CreateDoorOutcome.SectionNotFound();
        }
        catch (AbwabSectionParentMismatchException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "sectionParentMismatch");
            return new CreateDoorOutcome.SectionParentMismatch();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new CreateDoorOutcome.DuplicateName();
        }
    }
}
