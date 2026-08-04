using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Commands.Sections.DeleteSection;

public sealed class DeleteSectionHandler(
    ILogger<DeleteSectionHandler> logger,
    IAbwabSectionsWriter writer)
{
    private const string FeatureName = "AbwabSections";
    private const string OperationName = "DeleteSection";

    public async Task<DeleteSectionOutcome> HandleAsync(DeleteSectionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        AbwabSectionDeleteResult result;
        try
        {
            result = await writer.DeleteAsync(command.Id, cancellationToken);
        }
        catch (AbwabStaleVersionException)
        {
            logger.LogWarning("Rejected {feature} {operation} {reason} {sectionId}", FeatureName, OperationName, "staleVersion", command.Id);
            return new DeleteSectionOutcome.StaleVersion();
        }

        switch (result)
        {
            case AbwabSectionDeleteResult.Deleted:
                logger.LogInformation("Completed {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
                return new DeleteSectionOutcome.Success();
            case AbwabSectionDeleteResult.NotFound:
                logger.LogWarning("Not found {feature} {operation} {sectionId}", FeatureName, OperationName, command.Id);
                return new DeleteSectionOutcome.NotFound();
            case AbwabSectionDeleteResult.HasLiveDoors:
                logger.LogWarning("Rejected {feature} {operation} {reason} {sectionId}", FeatureName, OperationName, "hasLiveDoors", command.Id);
                return new DeleteSectionOutcome.HasLiveDoors();
            default:
                throw new InvalidOperationException($"Unhandled {nameof(AbwabSectionDeleteResult)} variant.");
        }
    }
}
