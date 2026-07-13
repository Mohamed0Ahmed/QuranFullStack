using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;

public sealed class GetWordTypeGroupedSummaryHandler(
    ILogger<GetWordTypeGroupedSummaryHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeGroupedSummary";

    public async Task<GetWordTypeGroupedSummaryOutcome> HandleAsync(
        GetWordTypeGroupedSummaryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!WordTypeGroupedDimensionKindParser.TryParse(query.Kind, out var kind))
        {
            LogRejected("invalidKind", query.Kind, query.DimensionId, query);
            return new GetWordTypeGroupedSummaryOutcome.InvalidKind();
        }

        if (query.DimensionId <= 0)
        {
            LogRejected("invalidId", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedSummaryOutcome.InvalidId();
        }

        var filter = WordTypesHandlerValidation.NormalizeFilter(
            query.Type, query.ChildCode, query.Case, query.Tense, query.Voice);
        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            LogRejected("invalidFilter", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedSummaryOutcome.InvalidFilter();
        }

        var summary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(kind, query.DimensionId, filter),
            cancellationToken);
        if (summary is null)
        {
            return new GetWordTypeGroupedSummaryOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {dimensionId} {type} {childCode}",
            FeatureName,
            OperationName,
            kind.ToRouteKey(),
            query.DimensionId,
            filter.Type,
            filter.ChildCode);

        return new GetWordTypeGroupedSummaryOutcome.Success(summary);
    }

    private void LogRejected(string reason, string? kind, int dimensionId, GetWordTypeGroupedSummaryQuery query)
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
