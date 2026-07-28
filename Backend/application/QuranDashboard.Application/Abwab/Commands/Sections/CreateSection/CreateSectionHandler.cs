using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Sections.CreateSection;

public sealed class CreateSectionHandler(
    ILogger<CreateSectionHandler> logger,
    IAbwabSectionsWriter writer)
{
    private const string FeatureName = "AbwabSections";
    private const string OperationName = "CreateSection";

    public async Task<CreateSectionOutcome> HandleAsync(CreateSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new CreateSectionOutcome.InvalidName();
        }

        try
        {
            var section = await writer.CreateAsync(name, cancellationToken);
            logger.LogInformation("Completed {feature} {operation} {sectionId}", FeatureName, OperationName, section.Id);
            return new CreateSectionOutcome.Success(section);
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new CreateSectionOutcome.DuplicateName();
        }
    }
}
