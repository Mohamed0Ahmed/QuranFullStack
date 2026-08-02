using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Templates.CreateTemplate;

public sealed class CreateTemplateHandler(
    ILogger<CreateTemplateHandler> logger,
    IAbwabTemplatesWriter writer)
{
    private const string FeatureName = "AbwabTemplates";
    private const string OperationName = "CreateTemplate";

    public async Task<CreateTemplateOutcome> HandleAsync(
        CreateTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new CreateTemplateOutcome.InvalidName();
        }

        var template = await writer.CreateAsync(
            name,
            command.Description,
            command.RepresentativeAyahText,
            command.Aliases ?? [],
            cancellationToken);

        logger.LogInformation("Completed {feature} {operation} {templateId}", FeatureName, OperationName, template.Id);
        return new CreateTemplateOutcome.Success(template);
    }
}
