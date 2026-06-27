using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;

public sealed class GetLemmaMissingSurahsHandler(
    ILogger<GetLemmaMissingSurahsHandler> logger,
    ILemmasReader reader)
{
    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaMissingSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "missing";

    public async Task<GetLemmaMissingSurahsOutcome> HandleAsync(
        GetLemmaMissingSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {subView} {lemmaId}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                SubViewName,
                query.Id);

            return new GetLemmaMissingSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var missing = await reader.GetLemmaMissingSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (missing is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {lemmaId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetLemmaMissingSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {lemmaId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            missing.Surahs.Count,
            missing.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetLemmaMissingSurahsOutcome.Success(missing);
    }
}
