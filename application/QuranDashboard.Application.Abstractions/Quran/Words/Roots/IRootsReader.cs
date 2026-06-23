using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.Roots;

/// <summary>
/// Application read boundary for the Roots Explorer (Feature 015). Read-only.
/// Mirrors the Feature 014 <c>IUniqueWordsReader</c> shape and conventions.
/// </summary>
/// <remarks>
/// <see langword="null"/> returns for single-root reads mean the id does not
/// exist; handlers map that to a controlled <c>404</c>.
/// </remarks>
public interface IRootsReader
{
    Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RootSummaryDto?> GetRootSummaryAsync(
        int id,
        CancellationToken cancellationToken);

    Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <remarks>
    /// <see cref="RootAyahMatchDto.MatchedQuranWordIds"/> holds exact
    /// <c>quran_words.id</c> values for ID-based highlighting.
    /// </remarks>
    Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootLemmasResponse?> GetRootLemmasAsync(
        int id,
        CancellationToken cancellationToken);

    Task<RootStemsResponse?> GetRootStemsAsync(
        int id,
        CancellationToken cancellationToken);
}
