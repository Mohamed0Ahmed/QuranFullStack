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
        => _ef.GetStemWordsAsync(id, wordKind, page, pageSize, cancellationToken);

    public Task<PagedResult<StemAyahMatchDto>?> GetStemAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetStemAyahMatchesAsync(id, page, pageSize, cancellationToken);

    public Task<StemSurahsResponse?> GetStemMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetStemMentionedSurahsAsync(id, cancellationToken);

    public Task<StemMissingSurahsResponse?> GetStemMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetStemMissingSurahsAsync(id, cancellationToken);

    public Task<StemLemmasResponse?> GetStemLemmasAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetStemLemmasAsync(id, cancellationToken);

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
}
