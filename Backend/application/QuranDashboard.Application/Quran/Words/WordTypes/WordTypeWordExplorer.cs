using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Application.Quran.Words.WordTypes;

public sealed class WordTypeWordExplorer(
    ILogger<WordTypeWordExplorer> logger,
    IWordTypesReader reader)
{
    public async Task<WordTypeWordResult.Summary> GetSummaryAsync(
        int tashkeelWordId, string? contextCode, string? @case, string? tense, string? voice,
        CancellationToken cancellationToken)
    {
        var identity = CreateIdentity("GetWordTypeSummary", tashkeelWordId, contextCode, @case, tense, voice);
        if (identity is null)
        {
            return new WordTypeWordResult.Summary.InvalidIdentity();
        }

        var result = await reader.GetSummaryAsync(identity, cancellationToken);
        return result is null ? new WordTypeWordResult.Summary.NotFound() : new WordTypeWordResult.Summary.Success(result);
    }

    public async Task<WordTypeWordResult.Ayahs> GetAyahsAsync(
        int tashkeelWordId, string? contextCode, string? @case, string? tense, string? voice,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var identity = CreateIdentity("GetWordTypeAyahs", tashkeelWordId, contextCode, @case, tense, voice);
        if (identity is null)
        {
            return new WordTypeWordResult.Ayahs.InvalidIdentity();
        }

        var paging = WordTypeDetailPaging.Create(page, pageSize);
        if (paging is null)
        {
            return new WordTypeWordResult.Ayahs.InvalidPaging();
        }

        var result = await reader.GetAyahMatchesAsync(identity, paging, cancellationToken);
        return result is null ? new WordTypeWordResult.Ayahs.NotFound() : new WordTypeWordResult.Ayahs.Success(result);
    }

    public async Task<WordTypeWordResult.Surahs> GetSurahsAsync(
        int tashkeelWordId, string? contextCode, string? @case, string? tense, string? voice,
        CancellationToken cancellationToken)
    {
        var identity = CreateIdentity("GetWordTypeSurahs", tashkeelWordId, contextCode, @case, tense, voice);
        if (identity is null)
        {
            return new WordTypeWordResult.Surahs.InvalidIdentity();
        }

        var result = await reader.GetSurahsAsync(identity, cancellationToken);
        return result is null ? new WordTypeWordResult.Surahs.NotFound() : new WordTypeWordResult.Surahs.Success(result);
    }

    private WordTypeRowIdentity? CreateIdentity(
        string operation, int tashkeelWordId, string? contextCode, string? @case, string? tense, string? voice)
    {
        var identity = WordTypeRowIdentity.Create(tashkeelWordId, contextCode, @case, tense, voice);
        if (identity is null)
        {
            logger.LogWarning(
                "Rejected {feature} {operation} {reason} {tashkeelWordId} {hasContextCode}",
                "WordTypes", operation, "invalidIdentity", tashkeelWordId, !string.IsNullOrWhiteSpace(contextCode));
        }

        return identity;
    }
}
