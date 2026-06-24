using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;

/// <summary>
/// EF Core read service for the Roots Explorer (Feature 015). Read-only:
/// <c>AsNoTracking</c>, no writes, no migrations. Mirrors Feature 014
/// <c>EfUniqueWordsReader</c>.
/// </summary>
/// <remarks>
/// <para>
/// List + summary (US1, T022/T023) compute the eight aggregate counts from ONE
/// grouped aggregation over <c>quran_word_morphology</c> (the driving relation)
/// joined to <c>quran_words</c>. <c>quran_word_morphology</c> is one row per
/// readable word, so ayah markers never enter the set. The whole summary is the
/// source of truth for the list; search/sort/page are applied in memory by
/// <see cref="RootsListDerivation"/> over the cached whole.
/// </para>
/// <para>
/// <b>Lemmas use co-occurrence</b> (<c>COUNT(DISTINCT lemma_id)</c> via
/// morphology where <c>root_id = X</c>), NOT <c>quran_lemmas.root_id</c>
/// ownership. <c>occurrences</c> = <c>quran_roots.words_count</c>. Both are
/// pinned in the seed so the invariants hold.
/// </para>
/// <para>
/// Per-root detail reads (ayahs T035, words T044, surahs T053, lemmas/stems
/// T061) are filled in by their owning user stories; they still throw here.
/// </para>
/// </remarks>
public sealed class EfRootsReader(QuranDashboardDbContext db) : IRootsReader
{
    private readonly QuranDashboardDbContext _db = db;

    /// <inheritdoc />
    public async Task<PagedResult<RootListItemDto>> GetRootsPageAsync(
        string? search,
        RootSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToPage(all, search, sort, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<RootSummaryDto?> GetRootSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var all = await LoadWholeSummaryAsync(cancellationToken);
        return RootsListDerivation.ToSummary(all, id);
    }

    /// <inheritdoc />
    public Task<PagedResult<RootWordItemDto>?> GetRootWordsAsync(
        int id,
        RootWordKind wordKind,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<PagedResult<RootAyahMatchDto>?> GetRootAyahMatchesAsync(
        int id,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootSurahsResponse?> GetRootMentionedSurahsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootMissingSurahsResponse?> GetRootMissingSurahsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootLemmasResponse?> GetRootLemmasAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<RootStemsResponse?> GetRootStemsAsync(int id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Computes the whole roots summary once: one grouped aggregation over
    /// <c>quran_word_morphology</c> joined to <c>quran_words</c>, joined to
    /// <c>quran_roots</c> for occurrences + first-occurrence metadata. Produces
    /// all eight counts per root. The cache decorator caches this whole under
    /// <c>roots:summary:all</c> and the list/summary reads derive from it.
    /// </summary>
    internal async Task<IReadOnlyList<RootSummaryRow>> LoadWholeSummaryAsync(CancellationToken cancellationToken)
    {
        // One grouped aggregation over quran_word_morphology (the driving
        // relation) joined to quran_words, then joined to quran_roots for the
        // occurrences (words_count) + first-occurrence metadata. Column aliases
        // are double-quoted so PostgreSQL preserves the exact PascalCase casing
        // EF Core maps against RootSummaryRow. The fold map is a fixed literal
        // bound as a parameter; only it is parameterized (never user input).
        //
        // Lemmas use CO-OCCURRENCE (COUNT(DISTINCT lemma_id) via morphology),
        // never quran_lemmas.root_id ownership; distinct_lemmas_count mirrors it
        // via COALESCE so the column and the lemmas-tab count always agree.
        var sql = $"""
            SELECT
                r.id AS "{nameof(RootSummaryRow.Id)}",
                r.root_text AS "{nameof(RootSummaryRow.RootText)}",
                -- Root text is stored with inter-letter spaces (e.g. "ر ح م"); strip
                -- spaces so a query typed as "رحم" matches. Fold applied for symmetry.
                replace(translate(lower(r.root_text), @foldFrom, @foldTo), ' ', '') AS "{nameof(RootSummaryRow.NormalizedRootText)}",
                r.words_count AS "{nameof(RootSummaryRow.OccurrencesCount)}",
                agg.ayahs_count AS "{nameof(RootSummaryRow.AyahsCount)}",
                agg.surahs_count AS "{nameof(RootSummaryRow.SurahsCount)}",
                agg.simple_words_count AS "{nameof(RootSummaryRow.SimpleWordsCount)}",
                agg.tashkeel_words_count AS "{nameof(RootSummaryRow.TashkeelWordsCount)}",
                COALESCE(agg.distinct_lemmas_count, r.distinct_lemmas_count) AS "{nameof(RootSummaryRow.LemmasCount)}",
                agg.stems_count AS "{nameof(RootSummaryRow.StemsCount)}",
                concat_ws(':', a_first.surah_number::text, a_first.ayah_number::text) AS "{nameof(RootSummaryRow.FirstVerseKey)}",
                r.first_word_order_in_mushaf AS "{nameof(RootSummaryRow.FirstWordOrderInMushaf)}"
            FROM quran_roots r
            LEFT JOIN (
                SELECT
                    m.root_id AS rid,
                    COUNT(DISTINCT w.ayah_id) AS ayahs_count,
                    COUNT(DISTINCT w.surah_number) AS surahs_count,
                    COUNT(DISTINCT w.unique_simple_word_id) AS simple_words_count,
                    COUNT(DISTINCT w.unique_tashkeel_word_id) AS tashkeel_words_count,
                    COUNT(DISTINCT m.lemma_id) AS distinct_lemmas_count,
                    COUNT(DISTINCT m.stem_id) AS stems_count
                FROM quran_word_morphology m
                JOIN quran_words w ON w.id = m.quran_word_id
                WHERE m.root_id IS NOT NULL
                GROUP BY m.root_id
            ) agg ON agg.rid = r.id
            LEFT JOIN quran_words w_first
                ON w_first.id = (
                    SELECT m2.quran_word_id
                    FROM quran_word_morphology m2
                    WHERE m2.root_id = r.id
                    ORDER BY m2.quran_word_id
                    LIMIT 1
                )
            LEFT JOIN quran_ayahs a_first ON a_first.id = w_first.ayah_id
            """;

        var rows = await _db.Database.SqlQueryRaw<RootSummaryRow>(
            sql,
            new NpgsqlParameter("foldFrom", RootsListDerivation.ArabicFoldFrom),
            new NpgsqlParameter("foldTo", RootsListDerivation.ArabicFoldTo))
            .ToListAsync(cancellationToken);

        return rows;
    }
}
