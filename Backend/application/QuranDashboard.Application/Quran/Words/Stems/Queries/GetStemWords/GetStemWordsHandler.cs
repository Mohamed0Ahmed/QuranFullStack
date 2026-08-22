using QuranDashboard.Application.Abstractions.Quran.Words.Stems;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;

public sealed class GetStemWordsHandler(
    ILogger<GetStemWordsHandler> logger,
    IStemsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Stems";
    private const string OperationName = "GetStemWords";
    private const string ViewName = "words";

    public async Task<GetStemWordsOutcome> HandleAsync(
        GetStemWordsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {stemId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetStemWordsOutcome.InvalidId();
        }

        if (!StemWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {stemId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidKind",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetStemWordsOutcome.InvalidKind();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {stemId} {subView} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize);

            return new GetStemWordsOutcome.InvalidPaging();
        }

        var stopwatch = Stopwatch.StartNew();
        var page = await reader.GetStemWordsAsync(
            query.Id,
            kind,
            query.TypeCode,
            query.Page,
            query.PageSize,
            cancellationToken);
        stopwatch.Stop();

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {stemId} {subView} {pageNumber} {pageSize} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize,
                stopwatch.ElapsedMilliseconds);

            return new GetStemWordsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {stemId} {subView} {pageNumber} {pageSize} {totalCount} {itemCount} {elapsedMs}",
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

        return new GetStemWordsOutcome.Success(page);
    }

    private static string GetKindKey(StemWordKind kind) =>
        kind == StemWordKind.Tashkeel
            ? StemWordKindKeys.Tashkeel
            : StemWordKindKeys.Simple;
}
