using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.DeleteTemplate;

public sealed class DeleteTemplateHandler(
    ILogger<DeleteTemplateHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "DeleteTemplate";

    public async Task<DeleteTemplateOutcome> HandleAsync(
        DeleteTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var deleted = await writer.DeleteAsync(command.TemplateId, cancellationToken);
        if (!deleted)
        {
            logger.LogWarning("Not found {feature} {operation} {templateId}", FeatureName, OperationName, command.TemplateId);
            return new DeleteTemplateOutcome.NotFound();
        }

        logger.LogInformation("Completed {feature} {operation} {templateId}", FeatureName, OperationName, command.TemplateId);
        return new DeleteTemplateOutcome.Success();
    }
}
