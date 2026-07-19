using QuranDashboard.Application.Abstractions.Quran.Words.Roots;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

public sealed class GetRootWordsHandler(
    ILogger<GetRootWordsHandler> logger,
    IRootsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Roots";
    private const string OperationName = "GetRootWords";
    private const string ViewName = "words";

    public async Task<GetRootWordsOutcome> HandleAsync(
        GetRootWordsQuery query,
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

            return new GetRootWordsOutcome.InvalidId();
        }

        if (!RootWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {rootId} {subView} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidKind",
                ViewName,
                query.Id,
                query.Kind ?? string.Empty,
                query.Page,
                query.PageSize);

            return new GetRootWordsOutcome.InvalidKind();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {rootId} {subView} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize);

            return new GetRootWordsOutcome.InvalidPaging();
        }

        var stopwatch = Stopwatch.StartNew();
        var page = await reader.GetRootWordsAsync(
            query.Id,
            kind,
            query.Page,
            query.PageSize,
            cancellationToken);
        stopwatch.Stop();

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {rootId} {subView} {pageNumber} {pageSize} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize,
                stopwatch.ElapsedMilliseconds);

            return new GetRootWordsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {rootId} {subView} {pageNumber} {pageSize} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            query.Id,
            GetKindKey(kind),
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetRootWordsOutcome.Success(page);
    }

    private static string GetKindKey(RootWordKind kind) =>
        kind == RootWordKind.Tashkeel ? RootWordKindKeys.Tashkeel : RootWordKindKeys.Simple;
}
