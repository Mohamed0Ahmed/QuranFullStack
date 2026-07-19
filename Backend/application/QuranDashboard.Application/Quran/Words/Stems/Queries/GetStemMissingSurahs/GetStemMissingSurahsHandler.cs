using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMissingSurahs;

public sealed class GetStemMissingSurahsHandler(
    ILogger<GetStemMissingSurahsHandler> logger,
    IStemsReader reader)
{
    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemMissingSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "missing";

    public async Task<GetStemMissingSurahsOutcome> HandleAsync(
        GetStemMissingSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {subView} {stemId}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                SubViewName,
                query.Id);

            return new GetStemMissingSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var missing = await reader.GetStemMissingSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (missing is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {stemId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetStemMissingSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {stemId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            missing.Surahs.Count,
            missing.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetStemMissingSurahsOutcome.Success(missing);
    }
}
