using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;

public sealed class GetRootLemmasHandler(
    ILogger<GetRootLemmasHandler> logger,
    IRootsReader reader)
{
    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootLemmas";
    private const string ViewName = "lemmas";

    public async Task<GetRootLemmasOutcome> HandleAsync(
        GetRootLemmasQuery query,
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

            return new GetRootLemmasOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var lemmas = await reader.GetRootLemmasAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (lemmas is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {rootId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetRootLemmasOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {rootId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            query.Id,
            lemmas.Lemmas.Count,
            lemmas.Lemmas.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootLemmasOutcome.Success(lemmas);
    }
}
