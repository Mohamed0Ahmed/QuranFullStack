using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Linking;

internal static class LinkingWorkspaceExecution
{
    private const string FeatureName = "LinkingWorkspace";

    public static async Task<LinkingWorkspaceOutcome> RunAsync(
        ILogger logger,
        string operationName,
        int userId,
        Func<Task<LinkingWorkspaceDto>> operation)
    {
        try
        {
            var workspace = await operation();

            logger.LogInformation(
                "Completed {feature} {operation} {userId} {sourceCount}",
                FeatureName,
                operationName,
                userId,
                workspace.Sources.Count);

            return new LinkingWorkspaceOutcome.Success(workspace);
        }
        catch (LinkingWorkspaceViolationException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {userId} {violationCode} {violationField}",
                FeatureName,
                operationName,
                "invalidRequest",
                userId,
                exception.Violation.Code,
                exception.Violation.Field);

            return new LinkingWorkspaceOutcome.InvalidRequest(exception.Violation);
        }
        catch (LinkingInvalidDescriptorException exception)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {userId} {violationCode} {violationField}",
                FeatureName,
                operationName,
                "invalidDescriptor",
                userId,
                exception.Violation.Code,
                exception.Violation.Field);

            return new LinkingWorkspaceOutcome.InvalidDescriptor(exception.Violation);
        }
        catch (LinkingWorkspaceSourceNotFoundException exception)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {userId} {sourceId}",
                FeatureName,
                operationName,
                userId,
                exception.SourceId);

            return new LinkingWorkspaceOutcome.SourceNotFound(exception.SourceId);
        }
        catch (LinkingStaleVersionException)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {userId}",
                FeatureName,
                operationName,
                "staleVersion",
                userId);

            return new LinkingWorkspaceOutcome.StaleVersion();
        }
        catch (LinkingDuplicateContributionException)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {userId}",
                FeatureName,
                operationName,
                "duplicateSource",
                userId);

            return new LinkingWorkspaceOutcome.DuplicateSource();
        }
    }
}
