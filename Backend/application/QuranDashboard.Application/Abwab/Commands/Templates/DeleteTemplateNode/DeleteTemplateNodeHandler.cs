using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplateNode;

public sealed class DeleteTemplateNodeHandler(
    ILogger<DeleteTemplateNodeHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "DeleteTemplateNode";

    public async Task<DeleteTemplateNodeOutcome> HandleAsync(
        DeleteTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = await writer.DeleteNodeAsync(command.NodeId, cancellationToken);

        switch (result)
        {
            case AbwabTemplateNodeDeleteResult.Deleted:
                logger.LogInformation("Completed {feature} {operation} {nodeId}", FeatureName, OperationName, command.NodeId);
                return new DeleteTemplateNodeOutcome.Success();
            case AbwabTemplateNodeDeleteResult.NotFound:
                logger.LogWarning("Not found {feature} {operation} {nodeId}", FeatureName, OperationName, command.NodeId);
                return new DeleteTemplateNodeOutcome.NotFound();
            case AbwabTemplateNodeDeleteResult.IsRoot:
                logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "rootNode");
                return new DeleteTemplateNodeOutcome.IsRoot();
            default:
                throw new InvalidOperationException($"Unhandled {nameof(AbwabTemplateNodeDeleteResult)} variant.");
        }
    }
}
