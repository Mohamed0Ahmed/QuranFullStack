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
/// (research D2). Per-root detail caching lands in later user stories. Mirrors
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
    public Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetRootWordsAsync(id, wordKind, page, pageSize, cancellationToken);

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
    public Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
        => _ef.GetRootMentionedSurahsAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
        => _ef.GetRootMissingSurahsAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
        => _ef.GetRootLemmasAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
        => _ef.GetRootStemsAsync(id, cancellationToken);

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
