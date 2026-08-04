using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Relations.AddDoorRelations;

public sealed class AddDoorRelationsHandler(
    ILogger<AddDoorRelationsHandler> logger,
    IAbwabRelationsWriter writer)
{
    private const string FeatureName = "AbwabRelations";
    private const string OperationName = "AddDoorRelations";

    public async Task<AddDoorRelationsOutcome> HandleAsync(
        AddDoorRelationsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.TargetDoorIds.Count == 0)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "invalidRequest", command.DoorId);
            return new AddDoorRelationsOutcome.InvalidRequest();
        }

        if (!Enum.IsDefined(command.Type))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "invalidType", command.DoorId);
            return new AddDoorRelationsOutcome.InvalidType();
        }

        if (!IsDirectionValidFor(command.Type, command.Direction))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "invalidDirection", command.DoorId);
            return new AddDoorRelationsOutcome.InvalidDirection();
        }

        try
        {
            var relations = await writer.AddAsync(
                command.DoorId, command.Type, command.Direction, command.TargetDoorIds, cancellationToken);

            logger.LogInformation(
                "Completed {feature} {operation} {doorId} {relationCount}",
                FeatureName, OperationName, command.DoorId, relations.Count);

            return new AddDoorRelationsOutcome.Success(relations);
        }
        catch (AbwabRelationSelfException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "selfRelation", command.DoorId);
            return new AddDoorRelationsOutcome.SelfRelation();
        }
        catch (AbwabRelationArchivedDoorException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "archivedDoor", command.DoorId);
            return new AddDoorRelationsOutcome.ArchivedDoor();
        }
        catch (AbwabNotFoundException)
        {
            logger.LogWarning("Not found {feature} {operation} {doorId}", FeatureName, OperationName, command.DoorId);
            return new AddDoorRelationsOutcome.NotFound();
        }
        catch (AbwabRelationDuplicateException ex)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {doorId}", FeatureName, OperationName, "duplicate", command.DoorId);
            return new AddDoorRelationsOutcome.Duplicate(ex.DoorNames);
        }
    }

    private static bool IsDirectionValidFor(AbwabRelationType type, AbwabRelationDirection? direction) =>
        type == AbwabRelationType.Comprehensiveness
            ? direction is not null && Enum.IsDefined(direction.Value)
            : direction is null;
}
