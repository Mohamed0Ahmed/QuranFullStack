using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;

public sealed class GetStemLemmasHandler(
    ILogger<GetStemLemmasHandler> logger,
    IStemsReader reader)
{
    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemLemmas";

    public async Task<GetStemLemmasOutcome> HandleAsync(
        GetStemLemmasQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {stemId}",
                FeatureName,
                OperationName,
                "invalidId",
                query.Id);

            return new GetStemLemmasOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var lemmas = await reader.GetStemLemmasAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (lemmas is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {stemId} {elapsedMs}",
                FeatureName,
                OperationName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetStemLemmasOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {stemId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            query.Id,
            lemmas.Lemmas.Count,
            lemmas.Lemmas.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetStemLemmasOutcome.Success(lemmas);
    }
}
