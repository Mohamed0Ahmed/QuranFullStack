using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.EditTemplateNode;

public sealed class EditTemplateNodeHandler(
    ILogger<EditTemplateNodeHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "EditTemplateNode";

    public async Task<EditTemplateNodeOutcome> HandleAsync(
        EditTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new EditTemplateNodeOutcome.InvalidName();
        }

        try
        {
            var node = await writer.EditNodeAsync(
                command.NodeId,
                name,
                command.Description,
                command.RepresentativeAyahText,
                command.Aliases ?? [],
                cancellationToken);

            if (node is null)
            {
                logger.LogWarning("Not found {feature} {operation} {nodeId}", FeatureName, OperationName, command.NodeId);
                return new EditTemplateNodeOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {nodeId}", FeatureName, OperationName, node.Id);
            return new EditTemplateNodeOutcome.Success(node);
        }
        catch (AbwabTemplateNodeDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new EditTemplateNodeOutcome.DuplicateName();
        }
    }
}
