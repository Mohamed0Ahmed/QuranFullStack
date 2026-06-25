using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;

/// <summary>
/// Bounded cache decorator over <see cref="EfLemmasReader"/> for the Lemmas
/// Explorer (Feature 016). Decorates the concrete EF reader and uses the existing
/// shared <see cref="IMemoryCache"/>; no global cache configuration is applied.
/// The lemma catalogue caches the whole summary list
/// (<see cref="LemmasCacheKeys.SummaryAll"/>) once; search/sort/paging are
/// applied in memory so sort/page changes issue no new SQL commands. Detail
/// caches (ayahs/words/surahs/relationships) are layered in by later story
/// phases (T056/T068/T080/T090).
/// </summary>
public sealed class CachedLemmasReader(EfLemmasReader efReader, IMemoryCache cache) : ILemmasReader
{
    private readonly EfLemmasReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public async Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    public async Task<LemmaSummaryDto?> GetLemmaSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await GetOrLoadWholeSummaryAsync(cancellationToken);
        return LemmasListDerivation.ToSummary(all, id);
    }

    public Task<PagedResult<LemmaWordItemDto>?> GetLemmaWordsAsync(
        int id,
        LemmaWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetLemmaWordsAsync(id, wordKind, page, pageSize, cancellationToken);

    public Task<PagedResult<LemmaAyahMatchDto>?> GetLemmaAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetLemmaAyahMatchesAsync(id, page, pageSize, cancellationToken);

    public Task<LemmaSurahsResponse?> GetLemmaMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetLemmaMentionedSurahsAsync(id, cancellationToken);

    public Task<LemmaMissingSurahsResponse?> GetLemmaMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetLemmaMissingSurahsAsync(id, cancellationToken);

    public Task<LemmaStemsResponse?> GetLemmaStemsAsync(
        int id,
        CancellationToken cancellationToken)
        => _ef.GetLemmaStemsAsync(id, cancellationToken);

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
}
