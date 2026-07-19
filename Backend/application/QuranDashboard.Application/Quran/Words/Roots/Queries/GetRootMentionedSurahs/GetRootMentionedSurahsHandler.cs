using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;

public sealed class GetRootMentionedSurahsHandler(
    ILogger<GetRootMentionedSurahsHandler> logger,
    IRootsReader reader)
{
    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootMentionedSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "mentioned";

    public async Task<GetRootMentionedSurahsOutcome> HandleAsync(
        GetRootMentionedSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {subView} {rootId}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                SubViewName,
                query.Id);

            return new GetRootMentionedSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var surahs = await reader.GetRootMentionedSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (surahs is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {rootId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetRootMentionedSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {rootId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            surahs.Surahs.Count,
            surahs.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootMentionedSurahsOutcome.Success(surahs);
    }
}
