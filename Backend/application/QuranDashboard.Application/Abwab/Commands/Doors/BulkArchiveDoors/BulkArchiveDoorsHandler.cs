using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Doors.BulkArchiveDoors;

public sealed class BulkArchiveDoorsHandler(
    ILogger<BulkArchiveDoorsHandler> logger,
    IAbwabDoorsWriter writer)
{
    private const string FeatureName = "AbwabDoors";
    private const string OperationName = "BulkArchiveDoors";

    public async Task<BulkArchiveDoorsOutcome> HandleAsync(BulkArchiveDoorsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Doors.Count == 0 || command.Doors.Any(door => door is null))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidRequest");
            return new BulkArchiveDoorsOutcome.InvalidRequest();
        }

        try
        {
            var archivedIds = await writer.BulkArchiveAsync(command.Doors.Select(door => door!).ToList(), cancellationToken);
            logger.LogInformation("Completed {feature} {operation} {count}", FeatureName, OperationName, archivedIds.Count);
            return new BulkArchiveDoorsOutcome.Success(archivedIds);
        }
        catch (AbwabNotFoundException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "notFound");
            return new BulkArchiveDoorsOutcome.NotFound();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "staleVersion");
            return new BulkArchiveDoorsOutcome.StaleVersion();
        }
    }
}
