using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;

public sealed class GetLemmaWordsHandler(
    ILogger<GetLemmaWordsHandler> logger,
    ILemmasReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaWords";
    private const string ViewName = "words";

    public async Task<GetLemmaWordsOutcome> HandleAsync(
        GetLemmaWordsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {lemmaId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidId",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetLemmaWordsOutcome.InvalidId();
        }

        if (!LemmaWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {lemmaId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidKind",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetLemmaWordsOutcome.InvalidKind();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {lemmaId} {subView} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize);

            return new GetLemmaWordsOutcome.InvalidPaging();
        }

        var stopwatch = Stopwatch.StartNew();
        var page = await reader.GetLemmaWordsAsync(
            query.Id,
            kind,
            query.Page,
            query.PageSize,
            cancellationToken);
        stopwatch.Stop();

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {lemmaId} {subView} {pageNumber} {pageSize} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                GetKindKey(kind),
                query.Page,
                query.PageSize,
                stopwatch.ElapsedMilliseconds);

            return new GetLemmaWordsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {lemmaId} {subView} {pageNumber} {pageSize} {totalCount} {itemCount} {elapsedMs}",
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

        return new GetLemmaWordsOutcome.Success(page);
    }

    private static string GetKindKey(LemmaWordKind kind) =>
        kind == LemmaWordKind.Tashkeel
            ? LemmaWordKindKeys.Tashkeel
            : LemmaWordKindKeys.Simple;
}
