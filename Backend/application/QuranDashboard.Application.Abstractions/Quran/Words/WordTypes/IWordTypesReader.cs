using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

public interface IWordTypesReader
{
    Task<WordTypeTreeDto> GetTreeAsync(CancellationToken cancellationToken);

    Task<PagedResult<WordTypeRowDto>> GetRowsAsync(
        WordTypeFilter filter,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<WordTypeTableRowDto>> GetTableRowsAsync(
        WordTypeFilter filter,
        WordTypeTableView tableView,
        WordTypeSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    // Scoped four-count summary (Feature 026, US8): words + distinct roots/stems/lemmas for the full
    // active list scope carried by the filter (type, childCode, case, tense, voice, search, presence
    // flags). One SQL command; each count equals the corresponding tableView's TotalCount for the
    // identical scope. A valid scope with no rows returns an all-zero DTO.
    Task<WordTypeScopeCountsDto> GetScopeCountsAsync(
        WordTypeFilter filter,
        CancellationToken cancellationToken);

    Task<WordTypeSummaryDto?> GetSummaryAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);

    Task<PagedResult<WordTypeAyahMatchDto>?> GetAyahMatchesAsync(
        WordTypeRowIdentity identity,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<WordTypeSurahsResponse?> GetSurahsAsync(
        WordTypeRowIdentity identity,
        CancellationToken cancellationToken);

    // Grouped (root/stem/lemma) scoped summary. Returns null when the positive dimension ID does not
    // exist in the supplied scope. All membership and counts derive from head-level
    // quran_word_morphology, never quran_word_morphology_segments.
    Task<WordTypeGroupedSummaryDto?> GetGroupedSummaryAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken);

    // Grouped scoped member words: the Words table restricted to the selected numeric head dimension ID,
    // grouped by the identical (unique_tashkeel_word_id, context_code) formula and paged after grouping.
    // Returns null when the dimension is absent from the scope; an existing selection with an out-of-range
    // page returns a non-null empty page carrying the correct TotalCount.
    Task<PagedResult<WordTypeGroupedMemberWordDto>?> GetGroupedMemberWordsAsync(
        WordTypeGroupedSelection selection,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    // Grouped scoped ayahs: the distinct ayahs of the selected numeric head dimension ID, paged in Mushaf
    // order and hydrated with canonical quran_words.text_uthmani plus the scoped matched word ids/positions.
    // Returns null when the dimension is absent from the scope; an existing selection with an out-of-range
    // page returns a non-null empty page carrying the correct TotalCount.
    Task<PagedResult<WordTypeAyahMatchDto>?> GetGroupedAyahMatchesAsync(
        WordTypeGroupedSelection selection,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    // Grouped scoped surahs: single-shot mentioned + missing surah lists for the selected numeric head
    // dimension ID. Occurrence counts are aggregated inside the database over the same dimension-filtered
    // base; the mentioned/missing split is derived against the surah catalogue in numeric order. Returns
    // null when the dimension is absent from the scope. There is no paging contract for this read.
    Task<WordTypeSurahsResponse?> GetGroupedSurahsAsync(
        WordTypeGroupedSelection selection,
        CancellationToken cancellationToken);
}
