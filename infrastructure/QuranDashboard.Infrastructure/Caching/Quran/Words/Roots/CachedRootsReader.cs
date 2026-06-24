using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

/// <summary>
/// Decorates <see cref="EfRootsReader"/> with <c>IMemoryCache</c> caching using
/// the <c>roots:</c> namespace. The whole summary is computed once under
/// <c>roots:summary:all</c>; search, sort, and paging are derived in memory
/// (research D2). Per-root detail reads (ayahs, words, surahs, lemmas, stems)
/// are cached under the <c>roots:</c> namespace. Mirrors
/// Feature 014 <c>CachedUniqueWordsReader</c> conventions where applicable.
/// </summary>
public sealed class CachedRootsReader(EfRootsReader efReader, IMemoryCache cache) : IRootsReader
{
    private readonly EfRootsReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    /// <inheritdoc />
    public async Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToSummary(all, id);
    }

    /// <inheritdoc />
    public async Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Words(id, wordKind, page, pageSize);

        if (_cache.TryGetValue(key, out PagedResult<RootWordItemDto>? cached))
        {
            return cached;
        }

        var words = await _ef.GetRootWordsAsync(id, wordKind, page, pageSize, cancellationToken);
        if (words is not null)
        {
            _cache.Set(key, words);
        }

        return words;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Ayahs(id, page, pageSize);

        if (_cache.TryGetValue(key, out PagedResult<RootAyahMatchDto>? cached))
        {
            return cached;
        }

        var ayahs = await _ef.GetRootAyahMatchesAsync(id, page, pageSize, cancellationToken);
        if (ayahs is not null)
        {
            _cache.Set(key, ayahs);
        }

        return ayahs;
    }

    /// <inheritdoc />
    public async Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Surahs(id);

        if (_cache.TryGetValue(key, out RootSurahsResponse? cached))
        {
            return cached;
        }

        var surahs = await _ef.GetRootMentionedSurahsAsync(id, cancellationToken);
        if (surahs is not null)
        {
            _cache.Set(key, surahs);
        }

        return surahs;
    }

    /// <inheritdoc />
    public async Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Missing(id);

        if (_cache.TryGetValue(key, out RootMissingSurahsResponse? cached))
        {
            return cached;
        }

        var missing = await _ef.GetRootMissingSurahsAsync(id, cancellationToken);
        if (missing is not null)
        {
            _cache.Set(key, missing);
        }

        return missing;
    }

    /// <inheritdoc />
    public async Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Lemmas(id);

        if (_cache.TryGetValue(key, out RootLemmasResponse? cached))
        {
            return cached;
        }

        var lemmas = await _ef.GetRootLemmasAsync(id, cancellationToken);
        if (lemmas is not null)
        {
            _cache.Set(key, lemmas);
        }

        return lemmas;
    }

    /// <inheritdoc />
    public async Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.Stems(id);

        if (_cache.TryGetValue(key, out RootStemsResponse? cached))
        {
            return cached;
        }

        var stems = await _ef.GetRootStemsAsync(id, cancellationToken);
        if (stems is not null)
        {
            _cache.Set(key, stems);
        }

        return stems;
    }

    private async Task<IReadOnlyList<RootSummaryRow>> GetOrLoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(RootsCacheKeys.SummaryAll, out IReadOnlyList<RootSummaryRow>? cached))
        {
            return cached!;
        }

        var rows = await _ef.LoadWholeSummaryAsync(cancellationToken);
        _cache.Set(RootsCacheKeys.SummaryAll, rows);
        return rows;
    }
}
