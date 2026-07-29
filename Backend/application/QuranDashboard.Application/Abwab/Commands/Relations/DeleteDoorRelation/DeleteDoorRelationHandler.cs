using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Relations.DeleteDoorRelation;

public sealed class DeleteDoorRelationHandler(
    ILogger<DeleteDoorRelationHandler> logger,
    IAbwabRelationsWriter writer)
{
    private const string FeatureName = "AbwabRelations";
    private const string OperationName = "DeleteDoorRelation";

    public async Task<DeleteDoorRelationOutcome> HandleAsync(
        DeleteDoorRelationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var deleted = await writer.DeleteAsync(command.RelationId, cancellationToken);
        if (!deleted)
        {
            logger.LogWarning("Not found {feature} {operation} {relationId}", FeatureName, OperationName, command.RelationId);
            return new DeleteDoorRelationOutcome.NotFound();
        }

        logger.LogInformation("Completed {feature} {operation} {relationId}", FeatureName, OperationName, command.RelationId);
        return new DeleteDoorRelationOutcome.Success();
    }
}
