using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abwab.Queries.GetAbwabTree;

public sealed class GetAbwabTreeHandler(
    ILogger<GetAbwabTreeHandler> logger,
    IAbwabTreeReader reader)
{
    private const string FeatureName = "Abwab";
    private const string OperationName = "GetAbwabTree";

    public async Task<GetAbwabTreeOutcome> HandleAsync(GetAbwabTreeQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tree = await reader.GetTreeAsync(cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {sectionCount} {doorCount}",
            FeatureName, OperationName, tree.Sections.Count, tree.Doors.Count);

        return new GetAbwabTreeOutcome.Success(tree);
    }
}
