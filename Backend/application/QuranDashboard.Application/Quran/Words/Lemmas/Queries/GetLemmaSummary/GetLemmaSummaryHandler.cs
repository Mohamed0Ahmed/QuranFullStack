using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;

public sealed class GetLemmaSummaryHandler(
    ILogger<GetLemmaSummaryHandler> logger,
    ILemmasReader reader)
{
    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaSummary";

    public async Task<GetLemmaSummaryOutcome> HandleAsync(
        GetLemmaSummaryQuery query,
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

            return new GetLemmaSummaryOutcome.InvalidId();
        }

        var summary = await reader.GetLemmaSummaryAsync(query.Id, cancellationToken);
        if (summary is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {lemmaId}",
                FeatureName,
                OperationName,
                query.Id);

            return new GetLemmaSummaryOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {lemmaId} {typeDistributionCount} {occurrencesCount} {ayahsCount} {surahsCount} {stemsCount}",
            FeatureName,
            OperationName,
            summary.Id,
            summary.TypeDistribution.Count,
            summary.OccurrencesCount,
            summary.AyahsCount,
            summary.SurahsCount,
            summary.StemsCount);

        return new GetLemmaSummaryOutcome.Success(summary);
    }
}
