using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

public sealed class GetWordTypeScopeCountsHandler(
    ILogger<GetWordTypeScopeCountsHandler> logger,
    IWordTypesReader reader)
{
    private const string FeatureName = "WordTypes";
    private const string OperationName = "GetWordTypeScopeCounts";

    public async Task<GetWordTypeScopeCountsOutcome> HandleAsync(
        GetWordTypeScopeCountsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Same scope normalization + validation the rows/table handlers apply, so the four counts share
        // the identical scope contract as the tableViews they must equal. Search is normalized/collapsed
        // and only its presence (hasSearch) is ever logged — never the text.
        var search = WordTypesHandlerValidation.NormalizeSearch(query.Search);
        var filter = new WordTypeFilter(
            WordTypesHandlerValidation.NormalizeType(query.Type),
            Normalize(query.ChildCode),
            Normalize(query.Case),
            Normalize(query.Tense),
            Normalize(query.Voice),
            search,
            query.HasRoot,
            query.HasStem,
            query.HasLemma);

        if (!WordTypesHandlerValidation.IsValidFilter(filter))
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {type} {childCode} {hasCaseFilter} {hasTenseFilter} {hasVoiceFilter} {hasSearch}",
                FeatureName,
                OperationName,
                "invalidFilter",
                filter.Type,
                filter.ChildCode,
                filter.Case is not null,
                filter.Tense is not null,
                filter.Voice is not null,
                filter.Search is not null);

            return new GetWordTypeScopeCountsOutcome.InvalidFilter();
        }

        var counts = await reader.GetScopeCountsAsync(filter, cancellationToken);
        logger.LogInformation(
            "Completed {feature} {operation} {type} {childCode} {hasSearch} {wordsCount} {rootsCount} {stemsCount} {lemmasCount}",
            FeatureName,
            OperationName,
            filter.Type,
            filter.ChildCode,
            filter.Search is not null,
            counts.WordsCount,
            counts.RootsCount,
            counts.StemsCount,
            counts.LemmasCount);

        return new GetWordTypeScopeCountsOutcome.Success(counts);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
