using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.MoveDoor;

public sealed class MoveDoorHandler(
    ILogger<MoveDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "MoveDoor";

    public async Task<MoveDoorOutcome> HandleAsync(MoveDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var door = await writer.MoveAsync(
                command.Id, command.TargetSectionId, command.TargetParentId, command.Version, cancellationToken);

            if (door is null)
            {
                logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
                return new MoveDoorOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
            return new MoveDoorOutcome.Success(door);
        }
        catch (AbwabParentNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "parentNotFound", command.Id);
            return new MoveDoorOutcome.ParentNotFound();
        }
        catch (AbwabSectionRequiredException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "sectionRequired", command.Id);
            return new MoveDoorOutcome.SectionRequired();
        }
        catch (AbwabSectionNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "sectionNotFound", command.Id);
            return new MoveDoorOutcome.SectionNotFound();
        }
        catch (AbwabCycleException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "wouldCycle", command.Id);
            return new MoveDoorOutcome.WouldCycle();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new MoveDoorOutcome.StaleVersion();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "duplicateName", command.Id);
            return new MoveDoorOutcome.DuplicateName();
        }
    }
}
