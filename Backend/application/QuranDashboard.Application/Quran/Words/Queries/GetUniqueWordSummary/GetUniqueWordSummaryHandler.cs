using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

public sealed class GetUniqueWordSummaryHandler(
    ILogger<GetUniqueWordSummaryHandler> logger,
    IUniqueWordsReader reader)
{
    private const string FeatureName = "Words";
    private const string OperationName = "GetUniqueWordSummary";

    public async Task<GetUniqueWordSummaryOutcome> HandleAsync(
        GetUniqueWordSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {uniqueWordId}",
                FeatureName,
                OperationName,
                "invalidKind",
                query.Id);

            return new GetUniqueWordSummaryOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {uniqueWordId}",
                FeatureName,
                OperationName,
                "invalidId",
                GetKindKey(kind),
                query.Id);

            return new GetUniqueWordSummaryOutcome.InvalidId();
        }

        var summary = await reader.GetUniqueWordSummaryAsync(kind, query.Id, cancellationToken);
        if (summary is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {kind} {uniqueWordId}",
                FeatureName,
                OperationName,
                GetKindKey(kind),
                query.Id);

            return new GetUniqueWordSummaryOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {uniqueWordId} {occurrencesCount} {ayahsCount} {surahsCount} {missingSurahsCount}",
            FeatureName,
            OperationName,
            GetKindKey(kind),
            summary.Id,
            summary.OccurrencesCount,
            summary.AyahsCount,
            summary.SurahsCount,
            summary.MissingSurahsCount);

        return new GetUniqueWordSummaryOutcome.Success(summary);
    }

    private static string GetKindKey(UniqueWordKind kind) =>
        kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;
}
