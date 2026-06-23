using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <remarks>
/// <see langword="null"/> returns for single-word reads mean the ID does not exist
/// for this kind; handlers map that to a controlled <c>404</c>. Ayah markers are
/// excluded from occurrence and highlight data.
/// </remarks>
public interface IUniqueWordsReader
{
    Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    /// <remarks>
    /// <see cref="UniqueWordAyahMatchDto.MatchedQuranWordIds"/> holds exact
    /// <c>quran_words.id</c> values for ID-based highlighting.
    /// </remarks>
    Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind,
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
