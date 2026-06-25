using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;

public sealed class GetStemSummaryHandler(
    ILogger<GetStemSummaryHandler> logger,
    IStemsReader reader)
{
    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemSummary";

    public async Task<GetStemSummaryOutcome> HandleAsync(
        GetStemSummaryQuery query,
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

            return new GetStemSummaryOutcome.InvalidId();
        }

        var summary = await reader.GetStemSummaryAsync(query.Id, cancellationToken);
        if (summary is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {stemId}",
                FeatureName,
                OperationName,
                query.Id);

            return new GetStemSummaryOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {stemId} {dominantLemmaId} {dominantRootId} {dominantTypeCode} {otherTypesCount} {occurrencesCount} {ayahsCount} {surahsCount} {simpleWordsCount} {tashkeelWordsCount}",
            FeatureName,
            OperationName,
            summary.Id,
            summary.LemmaId,
            summary.RootId,
            summary.DominantType.Code,
            summary.OtherTypesCount,
            summary.OccurrencesCount,
            summary.AyahsCount,
            summary.SurahsCount,
            summary.SimpleWordsCount,
            summary.TashkeelWordsCount);

        return new GetStemSummaryOutcome.Success(summary);
    }
}
