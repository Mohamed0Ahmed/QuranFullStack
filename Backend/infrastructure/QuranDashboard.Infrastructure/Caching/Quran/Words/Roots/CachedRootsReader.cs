using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

public sealed class CachedRootsReader(EfRootsReader efReader, IMemoryCache cache) : IRootsReader
{
    private readonly EfRootsReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public async Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        RootsCountFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToPage(all, filter, search, sort, page, pageSize);
    }

    public async Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToSummary(all, id);
    }

    public async Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var grouped = await GetOrLoadGroupedWordsAsync(id, wordKind, cancellationToken);
        return grouped is null
            ? null
            : RootsWordsDerivation.ToPage(grouped, page, pageSize);
    }

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
            _cache.Set(key, ayahs, RootsCacheEntryOptions.PagedDetail());
        }

        return ayahs;
    }

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
            _cache.Set(key, surahs, RootsCacheEntryOptions.WholeDetail());
        }

        return surahs;
    }

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
            _cache.Set(key, missing, RootsCacheEntryOptions.WholeDetail());
        }

        return missing;
    }

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
            _cache.Set(key, lemmas, RootsCacheEntryOptions.WholeDetail());
        }

        return lemmas;
    }

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
            _cache.Set(key, stems, RootsCacheEntryOptions.WholeDetail());
        }

        return stems;
    }

    private async Task<IReadOnlyList<RootWordItemDto>?> GetOrLoadGroupedWordsAsync(
        int id,
        RootWordKind wordKind,
        CancellationToken cancellationToken)
    {
        var key = RootsCacheKeys.WordsAll(id, wordKind);

        if (_cache.TryGetValue(key, out IReadOnlyList<RootWordItemDto>? cached))
        {
            return cached;
        }

        var grouped = await _ef.LoadGroupedRootWordsAsync(id, wordKind, cancellationToken);
        if (grouped is not null)
        {
            _cache.Set(key, grouped, RootsCacheEntryOptions.GroupedWords());
        }

        return grouped;
    }

    private async Task<IReadOnlyList<RootSummaryRow>> GetOrLoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(RootsCacheKeys.SummaryAll, out IReadOnlyList<RootSummaryRow>? cached))
        {
            return cached!;
        }

        var rows = await _ef.LoadWholeSummaryAsync(cancellationToken);
        _cache.Set(RootsCacheKeys.SummaryAll, rows, RootsCacheEntryOptions.SummaryAll());
        return rows;
    }
}
