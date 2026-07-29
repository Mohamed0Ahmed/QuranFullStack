using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.ReorderTemplateNode;

public sealed class ReorderTemplateNodeHandler(
    ILogger<ReorderTemplateNodeHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "ReorderTemplateNode";

    public async Task<ReorderTemplateNodeOutcome> HandleAsync(
        ReorderTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var node = await writer.ReorderNodeAsync(command.NodeId, command.Position, cancellationToken);
            if (node is null)
            {
                logger.LogWarning("Not found {feature} {operation} {nodeId}", FeatureName, OperationName, command.NodeId);
                return new ReorderTemplateNodeOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {nodeId}", FeatureName, OperationName, node.Id);
            return new ReorderTemplateNodeOutcome.Success(node);
        }
        catch (AbwabTemplateRootNodeException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "rootHasNoSiblings");
            return new ReorderTemplateNodeOutcome.IsRoot();
        }
        catch (AbwabInvalidPositionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {position}", FeatureName, OperationName, "invalidPosition", command.Position);
            return new ReorderTemplateNodeOutcome.InvalidPosition();
        }
    }
}
