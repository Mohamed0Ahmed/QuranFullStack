using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Infrastructure.Caching.Quran.Words.Roots;

/// <summary>
/// Decorates <see cref="IRootsReader"/> with <c>IMemoryCache</c> caching using
/// the <c>roots:</c> namespace. Per-method caching is filled in by each user
/// story (T024 list+summary, T036 ayahs, T045 words, T054 surahs/missing,
/// T062 lemmas/stems). This foundational skeleton simply delegates to the inner
/// reader so the DI composition compiles before any story lands. Mirrors
/// Feature 014 <c>CachedUniqueWordsReader</c>.
/// </summary>
public sealed class CachedRootsReader(IRootsReader inner, IMemoryCache cache) : IRootsReader
{
    private readonly IRootsReader _inner = inner;
    private readonly IMemoryCache _cache = cache;

    /// <inheritdoc />
    public Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _inner.GetRootsPageAsync(search, sort, page, pageSize, cancellationToken);

    /// <inheritdoc />
    public Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
        => _inner.GetRootSummaryAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _inner.GetRootWordsAsync(id, wordKind, page, pageSize, cancellationToken);

    /// <inheritdoc />
    public Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
        => _inner.GetRootAyahMatchesAsync(id, page, pageSize, cancellationToken);

    /// <inheritdoc />
    public Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
        => _inner.GetRootMentionedSurahsAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
        => _inner.GetRootMissingSurahsAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
        => _inner.GetRootLemmasAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
        => _inner.GetRootStemsAsync(id, cancellationToken);
}
