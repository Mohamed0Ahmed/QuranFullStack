using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;

// Cache decorator over EfLemmasReader: the whole summary list is cached once and search/sort/paging run
// in memory, so sort/page changes issue no new SQL.
public sealed class CachedLemmasReader(EfLemmasReader efReader, IMemoryCache cache) : ILemmasReader
{
    private readonly EfLemmasReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public async Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSortSpec sort,
        LemmasCountFilter filter,
        LemmasAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToPage(all, filter, association, search, sort, page, pageSize);
    }

    public async Task<LemmaSummaryDto?> GetLemmaSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToSummary(all, id);
    }

    public async Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
        int id,
        LemmaWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWordGroupsAsync(id, wordKind, cancellationToken);
        return all is null ? null : EfLemmasReader.SliceLemmaWordsPage(all, page, pageSize);
    }

    public Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken)
    {
        var key = LemmasCacheKeys.Ayahs(id, page, pageSize, typeCode);

        if (_cache.TryGetValue(key, out PagedResult<LemmaAyahMatchDto>? cached))
        {
            return Task.FromResult<PagedResult<LemmaAyahMatchDto>?>(cached);
        }

        return GetAndCacheAyahsAsync(id, page, pageSize, typeCode, cancellationToken, key);
    }

    public Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = LemmasCacheKeys.Surahs(id);

        if (_cache.TryGetValue(key, out LemmaSurahsResponse? cached))
        {
            return Task.FromResult<LemmaSurahsResponse?>(cached);
        }

        return GetAndCacheMentionedSurahsAsync(id, cancellationToken, key);
    }

    public Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = LemmasCacheKeys.Missing(id);

        if (_cache.TryGetValue(key, out LemmaMissingSurahsResponse? cached))
        {
            return Task.FromResult<LemmaMissingSurahsResponse?>(cached);
        }

        return GetAndCacheMissingSurahsAsync(id, cancellationToken, key);
    }

    public Task<LemmaStemsResponse?> GetLemmaStemsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var key = LemmasCacheKeys.Stems(id);

        if (_cache.TryGetValue(key, out LemmaStemsResponse? cached))
        {
            return Task.FromResult<LemmaStemsResponse?>(cached);
        }

        return GetAndCacheStemsAsync(id, cancellationToken, key);
    }

    private async Task<IReadOnlyList<LemmaSummaryRow>> GetOrLoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(LemmasCacheKeys.SummaryAll, out IReadOnlyList<LemmaSummaryRow>? cached))
        {
            return cached!;
        }

        var rows = await _ef.LoadWholeSummaryAsync(cancellationToken);
        _cache.Set(LemmasCacheKeys.SummaryAll, rows, LemmasCacheEntryOptions.SummaryAll());
        return rows;
    }

    private async Task<PagedResult<LemmaAyahMatchDto>?> GetAndCacheAyahsAsync(
        int id,
        int page,
        int pageSize,
        string? typeCode,
        CancellationToken cancellationToken,
        string key)
    {
        var ayahs = await _ef.GetLemmaAyahMatchesAsync(id, page, pageSize, typeCode, cancellationToken);
        if (ayahs is { Items.Count: > 0 })
        {
            _cache.Set(key, ayahs, LemmasCacheEntryOptions.PagedDetail());
        }

        return ayahs;
    }

    // Caches the complete grouped word list once per (lemma, kind); every page slices it in memory
    // instead of re-issuing the full occurrence query per page (perf finding B6). Concurrent cold
    // callers share one load via CacheLoadGate.
    private Task<IReadOnlyList<LemmaWordItemDto>?> GetOrLoadWordGroupsAsync(
        int id,
        LemmaWordKind wordKind,
        CancellationToken cancellationToken) =>
        CacheLoadGate.GetOrLoadAsync(
            _cache,
            LemmasCacheKeys.WordsAll(id, wordKind),
            ct => _ef.LoadLemmaWordGroupsAsync(id, wordKind, ct),
            LemmasCacheEntryOptions.GroupedWords,
            cancellationToken);

    private async Task<LemmaSurahsResponse?> GetAndCacheMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var surahs = await _ef.GetLemmaMentionedSurahsAsync(id, cancellationToken);
        if (surahs is not null)
        {
            _cache.Set(key, surahs, LemmasCacheEntryOptions.WholeDetail());
        }

        return surahs;
    }

    private async Task<LemmaMissingSurahsResponse?> GetAndCacheMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var missing = await _ef.GetLemmaMissingSurahsAsync(id, cancellationToken);
        if (missing is not null)
        {
            _cache.Set(key, missing, LemmasCacheEntryOptions.WholeDetail());
        }

        return missing;
    }

    private async Task<LemmaStemsResponse?> GetAndCacheStemsAsync(
        int id,
        CancellationToken cancellationToken,
        string key)
    {
        var stems = await _ef.GetLemmaStemsAsync(id, cancellationToken);
        if (stems is not null)
        {
            _cache.Set(key, stems, LemmasCacheEntryOptions.WholeDetail());
        }

        return stems;
    }
}
