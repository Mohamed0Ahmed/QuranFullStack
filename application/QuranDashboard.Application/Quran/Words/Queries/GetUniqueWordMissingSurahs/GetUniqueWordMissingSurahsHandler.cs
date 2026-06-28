using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;

public sealed class GetUniqueWordMissingSurahsHandler(
    ILogger<GetUniqueWordMissingSurahsHandler> logger,
    IUniqueWordsReader reader)
{
    private const string FeatureName = "Words";
    private const string OperationName = "GetUniqueWordMissingSurahs";

    public async Task<GetUniqueWordMissingSurahsOutcome> HandleAsync(
        GetUniqueWordMissingSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!UniqueWordKindParser.TryParse(query.Kind, out var kind))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {uniqueWordId}",
                FeatureName,
                OperationName,
                "invalidKind",
                query.Id);

            return new GetUniqueWordMissingSurahsOutcome.InvalidKind();
        }

        if (query.Id <= 0)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {kind} {uniqueWordId}",
                FeatureName,
                OperationName,
                "invalidId",
                GetKindKey(kind),
                query.Id);

            return new GetUniqueWordMissingSurahsOutcome.InvalidId();
        }

        var response = await reader.GetMissingSurahsAsync(kind, query.Id, cancellationToken);
        if (response is null)
        {
            logger.LogWarning(
                "Not found {feature} {operation} {kind} {uniqueWordId}",
                FeatureName,
                OperationName,
                GetKindKey(kind),
                query.Id);

            return new GetUniqueWordMissingSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {uniqueWordId} {missingSurahCount} {itemCount}",
            FeatureName,
            OperationName,
            GetKindKey(kind),
            query.Id,
            response.Surahs.Count,
            response.Surahs.Count);

        return new GetUniqueWordMissingSurahsOutcome.Success(response);
    }

    private static string GetKindKey(UniqueWordKind kind) =>
        kind == UniqueWordKind.Tashkeel
            ? UniqueWordKindKeys.Tashkeel
            : UniqueWordKindKeys.Simple;
}
