using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;

public sealed class GetRootStemsHandler(
    ILogger<GetRootStemsHandler> logger,
    IRootsReader reader)
{
    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootStems";
    private const string ViewName = "stems";

    public async Task<GetRootStemsOutcome> HandleAsync(
        GetRootStemsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {rootId}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                query.Id);

            return new GetRootStemsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var stems = await reader.GetRootStemsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (stems is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {rootId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetRootStemsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {rootId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            query.Id,
            stems.Stems.Count,
            stems.Stems.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootStemsOutcome.Success(stems);
    }
}
