using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

public sealed class GetWordTypeGroupedSurahsHandler(
    ILogger<GetWordTypeGroupedSurahsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeGroupedSurahs";

    public async Task<GetWordTypeGroupedSurahsOutcome> HandleAsync(
        GetWordTypeGroupedSurahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!WordTypeGroupedDimensionKindParser.TryParse(query.Kind, out var kind))
        {
            LogRejected("invalidKind", query.Kind, query.DimensionId, query);
            return new GetWordTypeGroupedSurahsOutcome.InvalidKind();
        }

        if (query.DimensionId <= 0)
        {
            LogRejected("invalidId", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedSurahsOutcome.InvalidId();
        }

        var filter = WordTypesHandlerValidation.NormalizeFilter(
            query.Type, query.ChildCode, query.Case, query.Tense, query.Voice);
        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            LogRejected("invalidFilter", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedSurahsOutcome.InvalidFilter();
        }

        var surahs = await reader.GetGroupedSurahsAsync(
            new WordTypeGroupedSelection(kind, query.DimensionId, filter),
            cancellationToken);
        if (surahs is null)
        {
            return new GetWordTypeGroupedSurahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {dimensionId} {type} {childCode} {mentionedCount} {missingCount}",
            FeatureName,
            OperationName,
            kind.ToRouteKey(),
            query.DimensionId,
            filter.Type,
            filter.ChildCode,
            surahs.Surahs.Count,
            surahs.MissingSurahs.Count);

        return new GetWordTypeGroupedSurahsOutcome.Success(surahs);
    }

    private void LogRejected(string reason, string? kind, int dimensionId, GetWordTypeGroupedSurahsQuery query)
    {
        logger.LogWarning(
            "Rejected {feature} {operation} {reason} {kind} {dimensionId} {type} {childCode} {hasCaseFilter} {hasTenseFilter} {hasVoiceFilter}",
            FeatureName,
            OperationName,
            reason,
            kind,
            dimensionId,
            WordTypesHandlerValidation.NormalizeType(query.Type),
            string.IsNullOrWhiteSpace(query.ChildCode) ? null : query.ChildCode.Trim(),
            !string.IsNullOrWhiteSpace(query.Case),
            !string.IsNullOrWhiteSpace(query.Tense),
            !string.IsNullOrWhiteSpace(query.Voice));
    }
}
