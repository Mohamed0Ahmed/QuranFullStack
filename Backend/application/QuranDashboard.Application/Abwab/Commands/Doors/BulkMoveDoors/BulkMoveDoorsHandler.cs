using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkMoveDoors;

public sealed class BulkMoveDoorsHandler(
    ILogger<BulkMoveDoorsHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "BulkMoveDoors";

    public async Task<BulkMoveDoorsOutcome> HandleAsync(BulkMoveDoorsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Doors.Count == 0 || command.Doors.Any(door => door is null))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidRequest");
            return new BulkMoveDoorsOutcome.InvalidRequest();
        }

        // All-or-nothing is intended, not a bug: every door's own concurrency token is checked inside
        // one SaveChanges, so a single stale row fails the whole batch rather than partially applying.
        try
        {
            var doors = await writer.BulkMoveAsync(
                command.Doors.Select(door => door!).ToList(), command.TargetSectionId, command.TargetParentId, cancellationToken);

            logger.LogInformation("Completed {feature} {operation} {count}", FeatureName, OperationName, doors.Count);
            return new BulkMoveDoorsOutcome.Success(doors);
        }
        catch (AbwabNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "notFound");
            return new BulkMoveDoorsOutcome.NotFound();
        }
        catch (AbwabParentNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "parentNotFound");
            return new BulkMoveDoorsOutcome.ParentNotFound();
        }
        catch (AbwabSectionRequiredException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "sectionRequired");
            return new BulkMoveDoorsOutcome.SectionRequired();
        }
        catch (AbwabSectionNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "sectionNotFound");
            return new BulkMoveDoorsOutcome.SectionNotFound();
        }
        catch (AbwabCycleException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "wouldCycle");
            return new BulkMoveDoorsOutcome.WouldCycle();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "staleVersion");
            return new BulkMoveDoorsOutcome.StaleVersion();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "duplicateName");
            return new BulkMoveDoorsOutcome.DuplicateName();
        }
    }
}
