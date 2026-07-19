using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words;

public sealed class CachedUniqueWordsReader(IUniqueWordsReader inner, IMemoryCache cache) : IUniqueWordsReader
{
    public async Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSortSpec sort,
        UniqueWordsCountFilter filter,
        UniqueWordsAssociationFilter association,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            return await inner.GetUniqueWordsPageAsync(kind, search, sort, filter, association, page, pageSize, cancellationToken);
        }

        var key = UniqueWordsCacheKeys.List(kind, sort, page, pageSize, filter, association);

        if (cache.TryGetValue(key, out PagedResult<UniqueWordListItemDto>? cached))
        {
            return cached!;
        }

        var result = await inner.GetUniqueWordsPageAsync(kind, search, sort, filter, association, page, pageSize, cancellationToken);
        cache.Set(key, result, UniqueWordsCacheEntryOptions.List());

        return result;
    }

    public async Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken)
    {
        var key = UniqueWordsCacheKeys.Summary(kind, id);

        if (cache.TryGetValue(key, out UniqueWordSummaryDto? cached))
        {
            return cached;
        }

        var summary = await inner.GetUniqueWordSummaryAsync(kind, id, cancellationToken);
        if (summary is not null)
        {
            cache.Set(key, summary, UniqueWordsCacheEntryOptions.Detail());
        }

        return summary;
    }

    public async Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken)
    {
        var key = UniqueWordsCacheKeys.Surahs(kind, id);

        if (cache.TryGetValue(key, out UniqueWordSurahsResponse? cached))
        {
            return cached;
        }

        var surahs = await inner.GetMentionedSurahsAsync(kind, id, cancellationToken);
        if (surahs is not null)
        {
            cache.Set(key, surahs, UniqueWordsCacheEntryOptions.Detail());
        }

        return surahs;
    }

    public async Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken)
    {
        var key = UniqueWordsCacheKeys.Missing(kind, id);

        if (cache.TryGetValue(key, out UniqueWordMissingSurahsResponse? cached))
        {
            return cached;
        }

        var missing = await inner.GetMissingSurahsAsync(kind, id, cancellationToken);
        if (missing is not null)
        {
            cache.Set(key, missing, UniqueWordsCacheEntryOptions.Detail());
        }

        return missing;
    }

    public async Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind,
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var key = UniqueWordsCacheKeys.Ayahs(kind, id, page, pageSize);

        if (cache.TryGetValue(key, out PagedResult<UniqueWordAyahMatchDto>? cached))
        {
            return cached;
        }

        var ayahs = await inner.GetAyahMatchesAsync(kind, id, page, pageSize, cancellationToken);
        if (ayahs is not null)
        {
            cache.Set(key, ayahs, UniqueWordsCacheEntryOptions.Detail());
        }

        return ayahs;
    }
}
