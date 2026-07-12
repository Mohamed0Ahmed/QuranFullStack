using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

public sealed class CachedWordTypesReader(EfWordTypesReader efReader, IMemoryCache cache) : IWordTypesReader
{
    private readonly EfWordTypesReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public async Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(WordTypesCacheKeys.Tree, out WordTypeTreeDto? cached))
        {
            return cached!;
        }

        var tree = await _ef.GetTreeAsync(cancellationToken);
        _cache.Set(WordTypesCacheKeys.Tree, tree, WordTypesCacheEntryOptions.Tree());
        return tree;
    }

    public async Task<PagedResult<WordTypeRowDto>> GetRowsAsync(WordTypeFilter filter, WordTypeSort sort, int page, int pageSize, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.Rows(filter, sort, page, pageSize);
        if (_cache.TryGetValue(key, out PagedResult<WordTypeRowDto>? cached))
        {
            return cached!;
        }

        var rows = await _ef.GetRowsAsync(filter, sort, page, pageSize, cancellationToken);
        _cache.Set(key, rows, WordTypesCacheEntryOptions.PagedRows());
        return rows;
    }

    public async Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(WordTypeFilter filter, WordTypeTableView tableView, WordTypeSort sort, int page, int pageSize, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.Table(filter, tableView, sort, page, pageSize);
        if (_cache.TryGetValue(key, out PagedResult<WordTypeTableRowDto>? cached))
        {
            return cached!;
        }

        var rows = await _ef.GetTableRowsAsync(filter, tableView, sort, page, pageSize, cancellationToken);
        _cache.Set(key, rows, WordTypesCacheEntryOptions.PagedRows());
        return rows;
    }

    public async Task<WordTypeSummaryDto?> GetSummaryAsync(WordTypeRowIdentity identity, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.Summary(identity);
        if (_cache.TryGetValue(key, out WordTypeSummaryDto? cached))
        {
            return cached;
        }

        var summary = await _ef.GetSummaryAsync(identity, cancellationToken);
        if (summary is not null)
        {
            _cache.Set(key, summary, WordTypesCacheEntryOptions.Detail());
        }

        return summary;
    }

    public async Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(WordTypeRowIdentity identity, int page, int pageSize, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.Ayahs(identity, page, pageSize);
        if (_cache.TryGetValue(key, out PagedResult<WordTypeAyahMatchDto>? cached))
        {
            return cached;
        }

        var ayahs = await _ef.GetAyahMatchesAsync(identity, page, pageSize, cancellationToken);
        if (ayahs is not null)
        {
            _cache.Set(key, ayahs, WordTypesCacheEntryOptions.Detail());
        }

        return ayahs;
    }

    public async Task<WordTypeSurahsResponse?> GetSurahsAsync(WordTypeRowIdentity identity, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.Surahs(identity);
        if (_cache.TryGetValue(key, out WordTypeSurahsResponse? cached))
        {
            return cached;
        }

        var surahs = await _ef.GetSurahsAsync(identity, cancellationToken);
        if (surahs is not null)
        {
            _cache.Set(key, surahs, WordTypesCacheEntryOptions.Detail());
        }

        return surahs;
    }

    public async Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(WordTypeGroupedSelection selection, CancellationToken cancellationToken)
    {
        var key = WordTypesCacheKeys.GroupedSummary(selection);
        if (_cache.TryGetValue(key, out WordTypeGroupedSummaryDto? cached))
        {
            return cached;
        }

        var summary = await _ef.GetGroupedSummaryAsync(selection, cancellationToken);
        if (summary is not null)
        {
            _cache.Set(key, summary, WordTypesCacheEntryOptions.Detail());
        }

        return summary;
    }
}
