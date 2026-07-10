using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;

public sealed class GetRootAyahsHandler(
    ILogger<GetRootAyahsHandler> logger,
    IRootsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootAyahs";
    private const string ViewName = "ayahs";

    public async Task<GetRootAyahsOutcome> HandleAsync(
        GetRootAyahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {rootId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetRootAyahsOutcome.InvalidId();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {rootId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetRootAyahsOutcome.InvalidPaging();
        }

        var stopwatch = Stopwatch.StartNew();
        var page = await reader.GetRootAyahMatchesAsync(
            query.Id,
            query.Page,
            query.PageSize,
            cancellationToken);
        stopwatch.Stop();

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {rootId} {pageNumber} {pageSize} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                query.Page,
                query.PageSize,
                stopwatch.ElapsedMilliseconds);

            return new GetRootAyahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {rootId} {pageNumber} {pageSize} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            query.Id,
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootAyahsOutcome.Success(page);
    }
}
