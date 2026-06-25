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
/// Caching behaviour for each read is layered in by the lemma story phases
/// (T034/T056/T068/T080/T090). This Phase 2 skeleton delegates straight through
/// to the EF reader so DI wiring and the test fixture can compile.
/// </summary>
public sealed class CachedLemmasReader(EfLemmasReader efReader, IMemoryCache cache) : ILemmasReader
{
    private readonly EfLemmasReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public Task<PagedResult<LemmaListItemDto>> GetLemmasPageAsync(
        string? search,
        LemmaSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetLemmasPageAsync(search, sort, page, pageSize, cancellationToken);

    public Task<LemmaSummaryDto?> GetLemmaSummaryAsync(int id, CancellationToken cancellationToken)
        => _ef.GetLemmaSummaryAsync(id, cancellationToken);

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
}
