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
/// Caching behaviour for each read is layered in by the stem story phases
/// (T045/T056/T068/T080/T090). This Phase 2 skeleton delegates straight through
/// to the EF reader so DI wiring and the test fixture can compile.
/// </summary>
public sealed class CachedStemsReader(EfStemsReader efReader, IMemoryCache cache) : IStemsReader
{
    private readonly EfStemsReader _ef = efReader;
    private readonly IMemoryCache _cache = cache;

    public Task<PagedResult<StemListItemDto>> GetStemsPageAsync(
        string? search,
        StemSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _ef.GetStemsPageAsync(search, sort, page, pageSize, cancellationToken);

    public Task<StemSummaryDto?> GetStemSummaryAsync(int id, CancellationToken cancellationToken)
        => _ef.GetStemSummaryAsync(id, cancellationToken);

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
}
