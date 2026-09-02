using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public sealed class WordTypeGroupedExplorer(
    ILogger<WordTypeGroupedExplorer> logger,
    IWordTypesReader reader)
{
    public async Task<WordTypeGroupedResult.Summary> GetSummaryAsync(
        string? kind, int dimensionId, string? type, string? childCode, string? @case, string? tense, string? voice,
        CancellationToken cancellationToken)
    {
        var parsedKind = WordTypeGroupedDimensionKind.Create(kind);
        if (parsedKind is null)
        {
            LogRejected("GetWordTypeGroupedSummary", "invalidKind", null, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Summary.InvalidKind();
        }

        if (dimensionId <= 0)
        {
            LogRejected("GetWordTypeGroupedSummary", "invalidId", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Summary.InvalidId();
        }

        var scope = WordTypeScope.Create(type, childCode, @case, tense, voice);
        if (scope is null)
        {
            LogRejected("GetWordTypeGroupedSummary", "invalidFilter", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Summary.InvalidFilter();
        }

        var selection = WordTypeGroupedSelection.Create(parsedKind, dimensionId, scope)!;
        var result = await reader.GetGroupedSummaryAsync(selection, cancellationToken);
        if (result is null)
        {
            return new WordTypeGroupedResult.Summary.NotFound();
        }

        logger.LogInformation("Completed {feature} {operation} {kind} {dimensionId} {type} {childCode}",
            "WordTypes", "GetWordTypeGroupedSummary", parsedKind.RouteKey, dimensionId, scope.Type, scope.ChildCode);
        return new WordTypeGroupedResult.Summary.Success(result);
    }

    public async Task<WordTypeGroupedResult.Words> GetWordsAsync(
        string? kind, int dimensionId, string? type, string? childCode, string? @case, string? tense, string? voice,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var parsedKind = WordTypeGroupedDimensionKind.Create(kind);
        if (parsedKind is null)
        {
            LogRejected("GetWordTypeGroupedWords", "invalidKind", null, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Words.InvalidKind();
        }
        if (dimensionId <= 0)
        {
            LogRejected("GetWordTypeGroupedWords", "invalidId", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Words.InvalidId();
        }
        var scope = WordTypeScope.Create(type, childCode, @case, tense, voice);
        if (scope is null)
        {
            LogRejected("GetWordTypeGroupedWords", "invalidFilter", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Words.InvalidFilter();
        }
        var paging = WordTypeDetailPaging.Create(page, pageSize);
        if (paging is null)
        {
            LogRejected("GetWordTypeGroupedWords", "invalidPaging", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Words.InvalidPaging();
        }

        var result = await reader.GetGroupedMemberWordsAsync(WordTypeGroupedSelection.Create(parsedKind, dimensionId, scope)!, paging, cancellationToken);
        if (result is null)
        {
            return new WordTypeGroupedResult.Words.NotFound();
        }
        logger.LogInformation("Completed {feature} {operation} {kind} {dimensionId} {type} {childCode} {pageNumber} {pageSize} {totalCount} {itemCount}",
            "WordTypes", "GetWordTypeGroupedWords", parsedKind.RouteKey, dimensionId, scope.Type, scope.ChildCode, paging.Page, paging.PageSize, result.TotalCount, result.Items.Count);
        return new WordTypeGroupedResult.Words.Success(result);
    }

    public async Task<WordTypeGroupedResult.Ayahs> GetAyahsAsync(
        string? kind, int dimensionId, string? type, string? childCode, string? @case, string? tense, string? voice,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var parsedKind = WordTypeGroupedDimensionKind.Create(kind);
        if (parsedKind is null)
        {
            LogRejected("GetWordTypeGroupedAyahs", "invalidKind", null, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Ayahs.InvalidKind();
        }
        if (dimensionId <= 0)
        {
            LogRejected("GetWordTypeGroupedAyahs", "invalidId", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Ayahs.InvalidId();
        }
        var scope = WordTypeScope.Create(type, childCode, @case, tense, voice);
        if (scope is null)
        {
            LogRejected("GetWordTypeGroupedAyahs", "invalidFilter", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Ayahs.InvalidFilter();
        }
        var paging = WordTypeDetailPaging.Create(page, pageSize);
        if (paging is null)
        {
            return new WordTypeGroupedResult.Ayahs.InvalidPaging();
        }

        var result = await reader.GetGroupedAyahMatchesAsync(WordTypeGroupedSelection.Create(parsedKind, dimensionId, scope)!, paging, cancellationToken);
        if (result is null)
        {
            return new WordTypeGroupedResult.Ayahs.NotFound();
        }
        logger.LogInformation("Completed {feature} {operation} {kind} {dimensionId} {type} {childCode} {pageNumber} {pageSize} {totalCount} {itemCount}",
            "WordTypes", "GetWordTypeGroupedAyahs", parsedKind.RouteKey, dimensionId, scope.Type, scope.ChildCode, paging.Page, paging.PageSize, result.TotalCount, result.Items.Count);
        return new WordTypeGroupedResult.Ayahs.Success(result);
    }

    public async Task<WordTypeGroupedResult.Surahs> GetSurahsAsync(
        string? kind, int dimensionId, string? type, string? childCode, string? @case, string? tense, string? voice,
        CancellationToken cancellationToken)
    {
        var parsedKind = WordTypeGroupedDimensionKind.Create(kind);
        if (parsedKind is null)
        {
            LogRejected("GetWordTypeGroupedSurahs", "invalidKind", null, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Surahs.InvalidKind();
        }
        if (dimensionId <= 0)
        {
            LogRejected("GetWordTypeGroupedSurahs", "invalidId", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Surahs.InvalidId();
        }
        var scope = WordTypeScope.Create(type, childCode, @case, tense, voice);
        if (scope is null)
        {
            LogRejected("GetWordTypeGroupedSurahs", "invalidFilter", parsedKind.RouteKey, dimensionId, type, childCode, @case, tense, voice);
            return new WordTypeGroupedResult.Surahs.InvalidFilter();
        }

        var result = await reader.GetGroupedSurahsAsync(WordTypeGroupedSelection.Create(parsedKind, dimensionId, scope)!, cancellationToken);
        if (result is null)
        {
            return new WordTypeGroupedResult.Surahs.NotFound();
        }
        logger.LogInformation("Completed {feature} {operation} {kind} {dimensionId} {type} {childCode} {mentionedCount} {missingCount}",
            "WordTypes", "GetWordTypeGroupedSurahs", parsedKind.RouteKey, dimensionId, scope.Type, scope.ChildCode, result.Surahs.Count, result.MissingSurahs.Count);
        return new WordTypeGroupedResult.Surahs.Success(result);
    }

    private void LogRejected(
        string operation, string reason, string? kind, int dimensionId,
        string? type, string? childCode, string? @case, string? tense, string? voice) =>
        logger.LogWarning(
            "Rejected {feature} {operation} {reason} {kind} {dimensionId} {hasType} {hasChildCode} {hasCaseFilter} {hasTenseFilter} {hasVoiceFilter}",
            "WordTypes", operation, reason, kind, dimensionId,
            !string.IsNullOrWhiteSpace(type),
            !string.IsNullOrWhiteSpace(childCode),
            !string.IsNullOrWhiteSpace(@case), !string.IsNullOrWhiteSpace(tense), !string.IsNullOrWhiteSpace(voice));
}
