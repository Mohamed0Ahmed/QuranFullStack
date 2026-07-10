using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;

public sealed class GetLemmaAyahsHandler(
    ILogger<GetLemmaAyahsHandler> logger,
    ILemmasReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 1000;

    private const string FeatureName = "Lemmas";
    private const string OperationName = "GetLemmaAyahs";
    private const string ViewName = "ayahs";

    public async Task<GetLemmaAyahsOutcome> HandleAsync(
        GetLemmaAyahsQuery query,
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

            return new GetLemmaAyahsOutcome.InvalidId();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {view} {lemmaId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                ViewName,
                query.Id,
                query.Page,
                query.PageSize);

            return new GetLemmaAyahsOutcome.InvalidPaging();
        }

        var stopwatch = Stopwatch.StartNew();
        var page = await reader.GetLemmaAyahMatchesAsync(
            query.Id,
            query.Page,
            query.PageSize,
            NormalizeTypeCode(query.TypeCode),
            cancellationToken);
        stopwatch.Stop();

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {view} {lemmaId} {pageNumber} {pageSize} {elapsedMs}",
                FeatureName,
                OperationName,
                ViewName,
                query.Id,
                query.Page,
                query.PageSize,
                stopwatch.ElapsedMilliseconds);

            return new GetLemmaAyahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {view} {lemmaId} {pageNumber} {pageSize} {totalCount} {itemCount} {elapsedMs}",
            FeatureName,
            OperationName,
            ViewName,
            query.Id,
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count,
            stopwatch.ElapsedMilliseconds);

        return new GetLemmaAyahsOutcome.Success(page);
    }

    private static string? NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();
}
