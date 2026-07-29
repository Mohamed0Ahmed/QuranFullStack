using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.ReorderDoor;

public sealed class ReorderDoorHandler(
    ILogger<ReorderDoorHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "ReorderDoor";

    public async Task<ReorderDoorOutcome> HandleAsync(ReorderDoorCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var door = await writer.ReorderAsync(command.Id, command.Position, command.Scope, command.Version, cancellationToken);
            if (door is null)
            {
                logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
                return new ReorderDoorOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {doorId}", FeatureName, OperationName, command.Id);
            return new ReorderDoorOutcome.Success(door);
        }
        catch (AbwabInvalidPositionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "invalidPosition", command.Id);
            return new ReorderDoorOutcome.InvalidPosition();
        }
        catch (AbwabScopeNotApplicableException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "scopeNotApplicable", command.Id);
            return new ReorderDoorOutcome.ScopeNotApplicable();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new ReorderDoorOutcome.StaleVersion();
        }
    }
}
