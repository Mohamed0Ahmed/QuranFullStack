using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;

public sealed class GetWordTypeGroupedAyahsHandler(
    ILogger<GetWordTypeGroupedAyahsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeGroupedAyahs";

    public async Task<GetWordTypeGroupedAyahsOutcome> HandleAsync(
        GetWordTypeGroupedAyahsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!WordTypeGroupedDimensionKindParser.TryParse(query.Kind, out var kind))
        {
            LogRejected("invalidKind", query.Kind, query.DimensionId, query);
            return new GetWordTypeGroupedAyahsOutcome.InvalidKind();
        }

        if (query.DimensionId <= 0)
        {
            LogRejected("invalidId", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedAyahsOutcome.InvalidId();
        }

        var filter = WordTypesHandlerValidation.NormalizeFilter(
            query.Type, query.ChildCode, query.Case, query.Tense, query.Voice);
        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            LogRejected("invalidFilter", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedAyahsOutcome.InvalidFilter();
        }

        if (!WordTypesHandlerValidation.IsValidDetailPaging(query.Page, query.PageSize))
        {
            return new GetWordTypeGroupedAyahsOutcome.InvalidPaging();
        }

        var page = await reader.GetGroupedAyahMatchesAsync(
            new WordTypeGroupedSelection(kind, query.DimensionId, filter),
            query.Page,
            query.PageSize,
            cancellationToken);
        if (page is null)
        {
            return new GetWordTypeGroupedAyahsOutcome.NotFound();
        }

        logger.LogInformation(
            "Completed {feature} {operation} {kind} {dimensionId} {type} {childCode} {pageNumber} {pageSize} {totalCount} {itemCount}",
            FeatureName,
            OperationName,
            kind.ToRouteKey(),
            query.DimensionId,
            filter.Type,
            filter.ChildCode,
            query.Page,
            query.PageSize,
            page.TotalCount,
            page.Items.Count);

        return new GetWordTypeGroupedAyahsOutcome.Success(page);
    }

    private void LogRejected(string reason, string? kind, int dimensionId, GetWordTypeGroupedAyahsQuery query)
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
