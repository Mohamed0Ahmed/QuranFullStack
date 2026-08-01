using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Sections.ReorderSection;

public sealed class ReorderSectionHandler(
    ILogger<ReorderSectionHandler> logger,
    IAbwabSectionsWriter writer)
{
    private const string FeatureName = "AbwabSections";
    private const string OperationName = "ReorderSection";

    public async Task<ReorderSectionOutcome> HandleAsync(ReorderSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var section = await writer.ReorderAsync(command.Id, command.Position, command.Version, cancellationToken);
            if (section is null)
            {
                logger.LogWarning("Not found {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
                return new ReorderSectionOutcome.NotFound();
            }

            logger.LogInformation("Completed {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
            return new ReorderSectionOutcome.Success(section);
        }
        catch (AbwabInvalidPositionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {sectionId}", FeatureName, OperationName, "invalidPosition", command.Id);
            return new ReorderSectionOutcome.InvalidPosition();
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {sectionId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new ReorderSectionOutcome.StaleVersion();
        }
    }
}
