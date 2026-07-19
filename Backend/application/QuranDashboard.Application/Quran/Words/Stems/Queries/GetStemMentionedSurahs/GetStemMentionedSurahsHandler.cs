using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMentionedSurahs;

public sealed class GetStemMentionedSurahsHandler(
    ILogger<GetStemMentionedSurahsHandler> logger,
    IStemsReader reader)
{
    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemMentionedSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "mentioned";

    public async Task<GetStemMentionedSurahsOutcome> HandleAsync(
        GetStemMentionedSurahsQuery query,
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

            return new GetStemMentionedSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var surahs = await reader.GetStemMentionedSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (surahs is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {stemId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetStemMentionedSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {stemId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            surahs.Surahs.Count,
            surahs.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetStemMentionedSurahsOutcome.Success(surahs);
    }
}
