using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;

public sealed class GetWordTypeTreeHandler(
    ILogger<GetWordTypeTreeHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeTree";

    public async Task<GetWordTypeTreeOutcome> HandleAsync(
        GetWordTypeTreeQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tree = await reader.GetTreeAsync(cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {itemCount}",
            FeatureName,
            OperationName,
            tree.MainTypes.Count);

        return new GetWordTypeTreeOutcome.Success(tree);
    }
}
