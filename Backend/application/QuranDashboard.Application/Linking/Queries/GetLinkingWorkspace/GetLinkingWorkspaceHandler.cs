using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Application.Linking.Queries.GetLinkingWorkspace;

public sealed class GetLinkingWorkspaceHandler(
    ILogger<GetLinkingWorkspaceHandler> logger,
    ILinkingWorkspaceReader reader)
{
    private const string OperationName = "GetLinkingWorkspace";

    public Task<LinkingWorkspaceOutcome> HandleAsync(
        GetLinkingWorkspaceQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return LinkingWorkspaceExecution.RunAsync(
            logger,
            OperationName,
            query.UserId,
            () => reader.LoadAsync(query.UserId, cancellationToken));
    }
}
