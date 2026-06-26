using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;

public sealed class GetLemmaStemsHandler(
    ILogger<GetLemmaStemsHandler> logger,
    ILemmasReader reader)
{
    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaStems";

    public async Task<GetLemmaStemsOutcome> HandleAsync(
        GetLemmaStemsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {lemmaId}",
                FeatureName,
                OperationName,
                "invalidId",
                query.Id);

            return new GetLemmaStemsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var stems = await reader.GetLemmaStemsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (stems is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {lemmaId} {elapsedMs}",
                FeatureName,
                OperationName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetLemmaStemsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {lemmaId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            query.Id,
            stems.StemsCount,
            stems.Stems.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetLemmaStemsOutcome.Success(stems);
    }
}
