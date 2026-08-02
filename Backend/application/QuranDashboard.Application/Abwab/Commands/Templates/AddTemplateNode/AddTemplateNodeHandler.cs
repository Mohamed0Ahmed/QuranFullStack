using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.AddTemplateNode;

public sealed class AddTemplateNodeHandler(
    ILogger<AddTemplateNodeHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "AddTemplateNode";

    public async Task<AddTemplateNodeOutcome> HandleAsync(
        AddTemplateNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new AddTemplateNodeOutcome.InvalidName();
        }

        if (command.ParentNodeId is not { } parentNodeId)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "missingParent");
            return new AddTemplateNodeOutcome.MissingParent();
        }

        try
        {
            var node = await writer.AddNodeAsync(
                command.TemplateId,
                parentNodeId,
                name,
                command.Description,
                command.RepresentativeAyahText,
                command.Aliases ?? [],
                cancellationToken);

            logger.LogInformation("Completed {feature} {operation} {nodeId}", FeatureName, OperationName, node.Id);
            return new AddTemplateNodeOutcome.Success(node);
        }
        catch (AbwabTemplateNotFoundException)
        {
            logger.LogWarning("Not found {feature} {operation} {templateId}", FeatureName, OperationName, command.TemplateId);
            return new AddTemplateNodeOutcome.TemplateNotFound();
        }
        catch (AbwabTemplateNodeNotFoundException)
        {
            logger.LogWarning("Not found {feature} {operation} {parentNodeId}", FeatureName, OperationName, parentNodeId);
            return new AddTemplateNodeOutcome.ParentNotFound();
        }
        catch (AbwabTemplateNodeDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new AddTemplateNodeOutcome.DuplicateName();
        }
    }
}
