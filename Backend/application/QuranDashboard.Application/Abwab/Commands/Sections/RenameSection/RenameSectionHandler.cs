using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Sections.RenameSection;

public sealed class RenameSectionHandler(
    ILogger<RenameSectionHandler> logger,
    IAbwabSectionsWriter writer)
{
    private const string FeatureName = "AbwabSections";
    private const string OperationName = "RenameSection";

    public async Task<RenameSectionOutcome> HandleAsync(RenameSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Rejected {feature} {operation} {reason}", FeatureName, OperationName, "invalidName");
            return new RenameSectionOutcome.InvalidName();
        }

        try
        {
            var section = await writer.RenameAsync(command.Id, name, command.Version, cancellationToken);
            if (section is null)
            {
                logger.LogWarning("Not found {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
                return new RenameSectionOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
            return new RenameSectionOutcome.Success(section);
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {sectionId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new RenameSectionOutcome.StaleVersion();
        }
        catch (AbwabDuplicateNameException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {name}", FeatureName, OperationName, "duplicateName", name);
            return new RenameSectionOutcome.DuplicateName();
        }
    }
}
