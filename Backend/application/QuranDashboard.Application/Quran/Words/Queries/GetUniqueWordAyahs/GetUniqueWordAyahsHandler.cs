using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;

public sealed class GetUniqueWordAyahsHandler(
    ILogger<GetUniqueWordAyahsHandler> logger,
    IUniqueWordsReader reader)
{
    public const int MinPage = 1;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    private const string FeatureName = "Words";
    private const string OperationName = "GetUniqueWordAyahs";

    public async Task<GetUniqueWordAyahsOutcome> HandleAsync(
        GetUniqueWordAyahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {uniqueWordId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidKind",
                query.Id,
                query.Page,
                query.PageSize);

            return new GetUniqueWordAyahsOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {uniqueWordId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidId",
                GetKindKey(kind),
                query.Id,
                query.Page,
                query.PageSize);

            return new GetUniqueWordAyahsOutcome.InvalidId();
        }

        if (query.Page < MinPage
            || query.PageSize < MinPageSize
            || query.PageSize > MaxPageSize)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {uniqueWordId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                "invalidPaging",
                GetKindKey(kind),
                query.Id,
                query.Page,
                query.PageSize);

            return new GetUniqueWordAyahsOutcome.InvalidPaging();
        }

        var page = await reader.GetAyahMatchesAsync(
            kind,
            query.Id,
            query.Page,
            query.PageSize,
            NormalizeTypeCode(query.TypeCode),
            cancellationToken);

        if (page is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {kind} {uniqueWordId} {pageNumber} {pageSize}",
                FeatureName,
                OperationName,
                GetKindKey(kind),
                query.Id,
                query.Page,
                query.PageSize);

            return new GetUniqueWordAyahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {uniqueWordId} {pageNumber} {pageSize} {totalCount} {itemCount}",
            FeatureName,
            OperationName,
            GetKindKey(kind),
            query.Id,
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count);

        return new GetUniqueWordAyahsOutcome.Success(page);
    }

    private static string GetKindKey(UniqueWordKind kind) =>
        kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;

    private static string? NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();
}
