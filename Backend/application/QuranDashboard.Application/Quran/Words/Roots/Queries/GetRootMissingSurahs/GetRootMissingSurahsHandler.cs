using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;

public sealed class GetRootMissingSurahsHandler(
    ILogger<GetRootMissingSurahsHandler> logger,
    IRootsReader reader)
{
    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootMissingSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "missing";

    public async Task<GetRootMissingSurahsOutcome> HandleAsync(
        GetRootMissingSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {subView} {rootId}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                SubViewName,
                query.Id);

            return new GetRootMissingSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var missing = await reader.GetRootMissingSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (missing is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {rootId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetRootMissingSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {rootId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            missing.Surahs.Count,
            missing.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootMissingSurahsOutcome.Success(missing);
    }
}
