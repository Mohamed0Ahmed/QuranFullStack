using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;

public sealed class GetRootSummaryHandler(
    ILogger<GetRootSummaryHandler> logger,
    IRootsReader reader)
{
    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootSummary";

    public async Task<GetRootSummaryOutcome> HandleAsync(
        GetRootSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {rootId}",
                FeatureName,
                OperationName,
                "invalidId",
                query.Id);

            return new GetRootSummaryOutcome.InvalidId();
        }

        var summary = await reader.GetRootSummaryAsync(query.Id, cancellationToken);
        if (summary is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {rootId}",
                FeatureName,
                OperationName,
                query.Id);

            return new GetRootSummaryOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {rootId} {occurrencesCount} {ayahsCount} {surahsCount} {lemmasCount} {stemsCount}",
            FeatureName,
            OperationName,
            summary.Id,
            summary.OccurrencesCount,
            summary.AyahsCount,
            summary.SurahsCount,
            summary.LemmasCount,
            summary.StemsCount);

        return new GetRootSummaryOutcome.Success(summary);
    }
}
