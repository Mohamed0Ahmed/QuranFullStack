using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.ApplyTemplate;

public sealed class ApplyTemplateHandler(
    ILogger<ApplyTemplateHandler> logger,
    IAbwabTemplateApplyWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "ApplyTemplate";

    public async Task<ApplyTemplateOutcome> HandleAsync(
        ApplyTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetDoorIds = command.TargetDoorIds ?? [];
        if (targetDoorIds.Count == 0)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "noTargets");
            return new ApplyTemplateOutcome.InvalidRequest();
        }

        try
        {
            var createdDoors = await writer.ApplyAsync(command.TemplateId, targetDoorIds, cancellationToken);

            logger.LogInformation(
                "Completed {feature} {operation} {templateId} {targetCount}",
                FeatureName, OperationName, command.TemplateId, createdDoors.Count);
            return new ApplyTemplateOutcome.Success(createdDoors);
        }
        catch (AbwabTemplateNotFoundException)
        {
            logger.LogWarning("Not found {feature} {operation} {templateId}", FeatureName, OperationName, command.TemplateId);
            return new ApplyTemplateOutcome.TemplateNotFound();
        }
        catch (AbwabNotFoundException)
        {
            logger.LogWarning("Not found {feature} {operation} {reason}", FeatureName, OperationName, "targetDoor");
            return new ApplyTemplateOutcome.TargetNotFound();
        }
        catch (AbwabTemplateTargetArchivedException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "targetArchived");
            return new ApplyTemplateOutcome.TargetArchived();
        }
        catch (AbwabTemplateEmptyException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "emptyTemplate");
            return new ApplyTemplateOutcome.EmptyTemplate();
        }
        catch (AbwabTemplateApplyCollisionException ex)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "nameCollision");
            return new ApplyTemplateOutcome.Collision(ex.DoorNames);
        }
    }
}
