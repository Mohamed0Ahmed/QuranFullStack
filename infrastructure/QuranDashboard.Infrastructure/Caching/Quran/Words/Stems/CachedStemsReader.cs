using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;

/// <summary>
/// Bounded cache decorator over <see cref="EfStemsReader"/> for the Stems
/// Explorer (Feature 016). Decorates the concrete EF reader and uses the existing
/// shared <see cref="IMemoryCache"/>; no global cache configuration is applied.
/// The whole summary list is cached once and reused for both catalogue search/
/// paging and selected-stem summary reads. Detail methods are still delegated to
/// later story phases.
/// </summary>
public sealed class CachedStemsReader(EfStemsReader efReader, IMemoryCache cache) : IStemsReader
{
    private readonly EfStemsReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public async Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return StemsListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    public async Task<StemSummaryDto?> GetStemSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Summary(id);

        if (_cache.TryGetValue(key, out StemSummaryDto? cached))
        {
            return cached;
        }

        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        var summary = StemsListDerivation.ToSummary(all, id);
        if (summary is not null)
        {
            _cache.Set(key, summary, StemsCacheEntryOptions.WholeDetail());
        }

        return summary;
    }

    public Task<PagedResult<StemWordItemDto>?> GetStemWordsAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Words(id, wordKind, page, pageSize);

        if (_cache.TryGetValue(key, out PagedResult<StemWordItemDto>? cached))
        {
            return Task.FromResult<PagedResult<StemWordItemDto>?>(cached);
        }

        return GetAndCacheWordsAsync(id, wordKind, page, pageSize, cancellationToken, key);
    }

    public Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Ayahs(id, page, pageSize);

        if (_cache.TryGetValue(key, out PagedResult<StemAyahMatchDto>? cached))
        {
            return Task.FromResult<PagedResult<StemAyahMatchDto>?>(cached);
        }

        return GetAndCacheAyahsAsync(id, page, pageSize, cancellationToken, key);
    }

    public Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Surahs(id);

        if (_cache.TryGetValue(key, out StemSurahsResponse? cached))
        {
            return Task.FromResult<StemSurahsResponse?>(cached);
        }

        return GetAndCacheMentionedSurahsAsync(id, cancellationToken, key);
    }

    public Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Missing(id);

        if (_cache.TryGetValue(key, out StemMissingSurahsResponse? cached))
        {
            return Task.FromResult<StemMissingSurahsResponse?>(cached);
        }

        return GetAndCacheMissingSurahsAsync(id, cancellationToken, key);
    }

    public Task<StemLemmasResponse?> GetStemLemmasAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = StemsCacheKeys.Lemmas(id);

        if (_cache.TryGetValue(key, out StemLemmasResponse? cached))
        {
            return Task.FromResult<StemLemmasResponse?>(cached);
        }

        return GetAndCacheLemmasAsync(id, cancellationToken, key);
    }

    private async Task<IReadOnlyList<StemSummaryRow>> GetOrLoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(StemsCacheKeys.SummaryAll, out IReadOnlyList<StemSummaryRow>? cached))
        {
            return cached!;
        }

        var rows = await _ef.LoadWholeSummaryAsync(cancellationToken);
        _cache.Set(StemsCacheKeys.SummaryAll, rows, StemsCacheEntryOptions.SummaryAll());
        return rows;
    }

    private async Task<PagedResult<StemAyahMatchDto>?> GetAndCacheAyahsAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string key)
    {
        var ayahs = await _ef.GetStemAyahMatchesAsync(id, page, pageSize, cancellationToken);
        if (ayahs is { Items.Count: > 0 })
        {
            _cache.Set(key, ayahs, StemsCacheEntryOptions.PagedDetail());
        }

        return ayahs;
    }

    private async Task<PagedResult<StemWordItemDto>?> GetAndCacheWordsAsync(
        int id,
        StemWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string key)
    {
        var words = await _ef.GetStemWordsAsync(id, wordKind, page, pageSize, cancellationToken);
        if (words is { Items.Count: > 0 })
        {
            _cache.Set(key, words, StemsCacheEntryOptions.PagedWords());
        }

        return words;
    }

    private async Task<StemSurahsResponse?> GetAndCacheMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var surahs = await _ef.GetStemMentionedSurahsAsync(id, cancellationToken);
        if (surahs is not null)
        {
            _cache.Set(key, surahs, StemsCacheEntryOptions.WholeDetail());
        }

        return surahs;
    }

    private async Task<StemMissingSurahsResponse?> GetAndCacheMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var missing = await _ef.GetStemMissingSurahsAsync(id, cancellationToken);
        if (missing is not null)
        {
            _cache.Set(key, missing, StemsCacheEntryOptions.WholeDetail());
        }

        return missing;
    }

    private async Task<StemLemmasResponse?> GetAndCacheLemmasAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var lemmas = await _ef.GetStemLemmasAsync(id, cancellationToken);
        if (lemmas is not null)
        {
            _cache.Set(key, lemmas, StemsCacheEntryOptions.WholeDetail());
        }

        return lemmas;
    }
}
