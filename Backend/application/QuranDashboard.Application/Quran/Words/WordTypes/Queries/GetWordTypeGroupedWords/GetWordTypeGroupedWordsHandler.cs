using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;

public sealed class GetWordTypeGroupedWordsHandler(
    ILogger<GetWordTypeGroupedWordsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeGroupedWords";

    public async Task<GetWordTypeGroupedWordsOutcome> HandleAsync(
        GetWordTypeGroupedWordsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!WordTypeGroupedDimensionKindParser.TryParse(query.Kind, out var kind))
        {
            LogRejected("invalidKind", query.Kind, query.DimensionId, query);
            return new GetWordTypeGroupedWordsOutcome.InvalidKind();
        }

        if (query.DimensionId <= 0)
        {
            LogRejected("invalidId", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedWordsOutcome.InvalidId();
        }

        var filter = WordTypesHandlerValidation.NormalizeFilter(
            query.Type, query.ChildCode, query.Case, query.Tense, query.Voice);
        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            LogRejected("invalidFilter", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedWordsOutcome.InvalidFilter();
        }

        if (!WordTypesHandlerValidation.IsValidPaging(query.Page, query.PageSize))
        {
            LogRejected("invalidPaging", kind.ToRouteKey(), query.DimensionId, query);
            return new GetWordTypeGroupedWordsOutcome.InvalidPaging();
        }

        var page = await reader.GetGroupedMemberWordsAsync(
            new WordTypeGroupedSelection(kind, query.DimensionId, filter),
            query.Page,
            query.PageSize,
            cancellationToken);
        if (page is null)
        {
            return new GetWordTypeGroupedWordsOutcome.NotFound();
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

        return new GetWordTypeGroupedWordsOutcome.Success(page);
    }

    private void LogRejected(string reason, string? kind, int dimensionId, GetWordTypeGroupedWordsQuery query)
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
