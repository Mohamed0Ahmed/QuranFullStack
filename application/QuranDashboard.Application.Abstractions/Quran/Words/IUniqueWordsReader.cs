using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words;

/// <summary>
/// Application read boundary for the Unique Words explorer (Feature 014).
/// Application handlers depend on this abstraction; Infrastructure implements
/// it against the existing read-only unique-word tables.
/// </summary>
/// <remarks>
/// All methods are read-only, no-tracking, and exclude ayah markers from
/// occurrence and highlight data. <see langword="null"/> return values for
/// single-word reads mean "well-formed ID does not exist for this kind", which
/// handlers map to a controlled <c>404</c>.
/// </remarks>
public interface IUniqueWordsReader
{
    /// <summary>
    /// Reads one bounded page of unique words for the given mode. Reads from the
    /// relevant unique-word table with precomputed counts; does not group
    /// <c>quran_words</c> per card.
    /// </summary>
    Task<PagedResult<UniqueWordListItemDto>> GetUniqueWordsPageAsync(
        UniqueWordKind kind,
        string? search,
        UniqueWordSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the summary of a selected unique word, used to restore modal state.
    /// Returns <see langword="null"/> when the ID does not exist for this kind.
    /// </summary>
    Task<UniqueWordSummaryDto?> GetUniqueWordSummaryAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the surahs where the selected unique word is mentioned, ordered by
    /// surah number, with the readable occurrence count per surah. Returns
    /// <see langword="null"/> when the ID does not exist for this kind.
    /// </summary>
    Task<UniqueWordSurahsResponse?> GetMentionedSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the 114-surah catalog minus the selected word's mentioned-surah
    /// set, ordered by surah number. Returns <see langword="null"/> when the ID
    /// does not exist for this kind; returns an empty list when the word
    /// appears in all surahs.
    /// </summary>
    Task<UniqueWordMissingSurahsResponse?> GetMissingSurahsAsync(
        UniqueWordKind kind,
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads one bounded page of distinct ayahs containing exact occurrences of
    /// the selected unique word. Follows the matched rows → paged ayah IDs →
    /// batched ayah words flow to avoid N+1 reads. <see cref="UniqueWordAyahMatchDto.MatchedQuranWordIds"/>
    /// holds exact <c>quran_words.id</c> values for ID-based highlighting.
    /// Returns <see langword="null"/> when the ID does not exist for this kind.
    /// </summary>
    Task<PagedResult<UniqueWordAyahMatchDto>?> GetAyahMatchesAsync(
        UniqueWordKind kind,
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
