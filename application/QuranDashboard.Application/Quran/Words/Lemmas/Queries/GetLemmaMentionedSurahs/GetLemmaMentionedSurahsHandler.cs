using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;

public sealed class GetLemmaMentionedSurahsHandler(
    ILogger<GetLemmaMentionedSurahsHandler> logger,
    ILemmasReader reader)
{
    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaMentionedSurahs";
    private const string ViewName = "surahs";
    private const string SubViewName = "mentioned";

    public async Task<GetLemmaMentionedSurahsOutcome> HandleAsync(
        GetLemmaMentionedSurahsQuery query,
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

            return new GetLemmaMentionedSurahsOutcome.InvalidId();
        }

        var stopwatch = Stopwatch.StartNew();
        var surahs = await reader.GetLemmaMentionedSurahsAsync(query.Id, cancellationToken);
        stopwatch.Stop();

        if (surahs is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {subView} {lemmaId} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                SubViewName,
                query.Id,
                stopwatch.ElapsedMilliseconds);

            return new GetLemmaMentionedSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {subView} {lemmaId} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            SubViewName,
            query.Id,
            surahs.SurahsCount,
            surahs.Surahs.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetLemmaMentionedSurahsOutcome.Success(surahs);
    }
}
