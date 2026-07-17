using Microsoft.EntityFrameworkCore.Storage;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

/// <summary>
/// The Word Types COUNT-column ORDER BY contract (Feature 030, N8), proved against real PostgreSQL for
/// both the words view and the grouped view.
/// <para>
/// WordTypesTableReadTests already pins the exact order of the mushaf-order and alpha columns. The count
/// columns were checked there only as a row set plus a total count, so a reversed — or column-swapped —
/// count arm stayed green. Every case below asserts the FULL ordered id sequence instead.
/// </para>
/// <para>
/// The shared fixture slice cannot prove this on its own: it has no scope in which occurrences, ayahs and
/// surahs disagree with each other, so an arm reading a neighbouring count column is invisible there.
/// Each test therefore seeds its own rows inside a transaction that is ALWAYS rolled back (the
/// <c>await using</c> disposal rolls back even when an assertion throws), leaving the slice every other
/// suite asserts on byte-identical.
/// </para>
/// <para>
/// The seeded rows are explicitly synthetic structural placeholders at non-canonical coordinates: ASCII
/// payloads and Arabic labels that name themselves as non-Quranic. They exist only to give the ORDER BY
/// distinguishable keys.
/// </para>
/// <para>
/// <b>On the final identity tie-break.</b> The locked contract chains equal primary values through mushaf
/// order and then Id. In Word Types that last rung is unreachable by construction, so no seam can force
/// it: both views derive <c>first_word_order_in_mushaf</c> as <c>MIN(quran_word_id)</c> over a GROUP BY
/// partition of <c>base</c>, and <c>base</c> holds exactly one row per word (quran_word_morphology is
/// keyed by quran_word_id and every join is on a PK, so nothing fans out). Distinct groups are therefore
/// disjoint sets of word ids and can never share a minimum. Unlike Unique Words — whose mushaf key is a
/// stored column guarded by a droppable UNIQUE index — there is no schema toggle that unlocks the rung.
/// The mushaf key alone already totally orders every tie group, which is exactly what
/// <see cref="Equal_counts_fall_through_to_mushaf_order_in_both_directions"/> proves.
/// </para>
/// </summary>
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesOrderingContractTests(WordTypesTestFixture fixture)
{
    // Two synthetic noun POS codes, each its own scope. Filtering on the child code pins head_pos, so a
    // test sees ONLY its own three rows and the two row sets below can share one seed without colliding.
    private const string DistinctCountsChildCode = "SYNO";
    private const string MushafTieChildCode = "SYNT";

    private const int ExpectedRowsPerScope = 3;

    // Groups A/B/C, in that order. Counts are a rotation, so each column × direction has its OWN
    // expected sequence: an arm reading a neighbouring count column lands somewhere else and fails.
    //        occurrences  ayahs  surahs   MIN(quran_word_id)
    //   A     6            2      2        194301
    //   B     5            4      1        194311
    //   C     4            3      3        194321
    private static readonly SyntheticGroup[] DistinctCountGroups =
    [
        new(TashkeelId: 194201, LemmaId: 194101),
        new(TashkeelId: 194202, LemmaId: 194102),
        new(TashkeelId: 194203, LemmaId: 194103),
    ];

    // Groups D/E/F, in that order. Every count is tied, so only the tie-break chain can order them, and
    // the mushaf key deliberately contradicts Id order: an Id-first tie-break would read D, E, F.
    //        occurrences  ayahs  surahs   MIN(quran_word_id)
    //   D     2            2      2        194341
    //   E     2            2      2        194331
    //   F     2            2      2        194351
    private static readonly SyntheticGroup[] MushafTieGroups =
    [
        new(TashkeelId: 194211, LemmaId: 194111),
        new(TashkeelId: 194212, LemmaId: 194112),
        new(TashkeelId: 194213, LemmaId: 194113),
    ];

    // Mushaf order over the tie groups: E (194331) then D (194341) then F (194351).
    private static readonly int[] MushafTieExpectedOrder = [1, 0, 2];

    // Every count column at both directions, with the group order each one must produce.
    private static readonly (string Sort, int[] GroupOrder)[] CountColumnExpectations =
    [
        ("occurrences", [0, 1, 2]),      // 6 > 5 > 4
        ("occurrences-asc", [2, 1, 0]),
        ("ayahs", [1, 2, 0]),            // 4 > 3 > 2
        ("ayahs-asc", [0, 2, 1]),
        ("surahs", [2, 0, 1]),           // 3 > 2 > 1
        ("surahs-asc", [1, 0, 2]),
    ];

    // The words view has its own ORDER BY; roots/stems/lemmas share ONE GroupedOrderBy, so lemmas stands
    // for the whole grouped arm.
    private static readonly WordTypeTableView[] OrderedViews =
    [
        WordTypeTableView.Words,
        WordTypeTableView.Lemmas,
    ];

    public static TheoryData<WordTypeTableView, string, int[]> CountColumnCases()
    {
        var cases = new TheoryData<WordTypeTableView, string, int[]>();
        foreach (var view in OrderedViews)
        {
            foreach (var (sort, groupOrder) in CountColumnExpectations)
            {
                cases.Add(view, sort, groupOrder);
            }
        }

        return cases;
    }

    public static TheoryData<WordTypeTableView, string> EveryCountTokenPerView()
    {
        var cases = new TheoryData<WordTypeTableView, string>();
        foreach (var view in OrderedViews)
        {
            foreach (var (sort, _) in CountColumnExpectations)
            {
                cases.Add(view, sort);
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(CountColumnCases))]
    public async Task Each_count_column_orders_by_its_own_count_in_both_directions(
        WordTypeTableView view,
        string sort,
        int[] expectedGroupOrder)
    {
        var ids = await OrderedRowIdsAsync(view, DistinctCountsChildCode, sort);

        ids.Should().Equal(ExpectedIds(view, DistinctCountGroups, expectedGroupOrder));
    }

    [Theory]
    [MemberData(nameof(EveryCountTokenPerView))]
    public async Task Equal_counts_fall_through_to_mushaf_order_in_both_directions(
        WordTypeTableView view,
        string sort)
    {
        var ids = await OrderedRowIdsAsync(view, MushafTieChildCode, sort);

        // Reversing the primary must never reshuffle a tie group, or a page boundary could drop or repeat
        // a row — so the SAME mushaf-order sequence is expected ascending and descending.
        ids.Should().Equal(ExpectedIds(view, MushafTieGroups, MushafTieExpectedOrder));
    }

    private async Task<IReadOnlyList<int>> OrderedRowIdsAsync(
        WordTypeTableView view,
        string childCode,
        string sort)
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        // Never committed: disposal rolls the seed back on every exit path.
        await using var transaction = await BeginSeededTransactionAsync(dbContext);

        // EfWordTypesReader, not IWordTypesReader: the cached decorator would cross-serve one case's
        // rolled-back rows to another. The reader resolves through the SAME scoped DbContext, so it reads
        // inside this transaction and sees the uncommitted rows.
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        WordTypeSortParser.TryParse(sort, out var spec).Should().BeTrue();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", childCode, null, null, null),
            view,
            spec,
            1,
            25,
            CancellationToken.None);

        page.TotalCount.Should().Be(
            ExpectedRowsPerScope,
            "the synthetic child code must scope the read to its own rows alone");

        return [.. page.Items.Select(RowId)];
    }

    private static int[] ExpectedIds(WordTypeTableView view, SyntheticGroup[] groups, int[] groupOrder) =>
        [.. groupOrder.Select(index => groups[index].RowIdFor(view))];

    private static int RowId(WordTypeTableRowDto row) => row switch
    {
        WordTableRowDto word => word.TashkeelWordId,
        LemmaTableRowDto lemma => lemma.LemmaId,
        _ => throw new ArgumentOutOfRangeException(nameof(row), row, "Only the words and lemmas views are seeded."),
    };

    private static async Task<IDbContextTransaction> BeginSeededTransactionAsync(QuranDashboardDbContext dbContext)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(SeedSql);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    // One synthetic group == one unique-tashkeel word AND one lemma, seeded 1:1 so the words view and the
    // grouped view partition the same rows into the same three groups with the same counts.
    private sealed record SyntheticGroup(int TashkeelId, int LemmaId)
    {
        public int RowIdFor(WordTypeTableView view) =>
            view == WordTypeTableView.Words ? TashkeelId : LemmaId;
    }

    // Test-only structural rows at non-canonical coordinates. quran_words is inserted with a null
    // unique_tashkeel_word_id and patched afterwards because the word and unique-word tables reference
    // each other.
    private const string SeedSql = """
        INSERT INTO quran_surahs
          (surah_number, name_arabic, name_simple, name_transliteration, revelation_place,
           revelation_order, verses_count, bismillah_pre)
        VALUES
          (32101, 'ترتيب هيكلي غير قرآني أ', 'Synthetic Ordering A', 'Synthetic Ordering A',
           'makkah', 32101, 4, FALSE),
          (32102, 'ترتيب هيكلي غير قرآني ب', 'Synthetic Ordering B', 'Synthetic Ordering B',
           'makkah', 32102, 1, FALSE),
          (32103, 'ترتيب هيكلي غير قرآني ج', 'Synthetic Ordering C', 'Synthetic Ordering C',
           'makkah', 32103, 1, FALSE);

        INSERT INTO quran_mushaf_pages
          (page_number, first_surah_number, first_ayah_number, last_surah_number, last_ayah_number, lines_count)
        VALUES
          (32100, 32101, 1, 32103, 1, 1);

        INSERT INTO quran_ayahs
          (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source,
           words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
        VALUES
          (194001, 32101, 1, 'synthetic-order:1:1', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           9, 9, 32100, 32100, NULL, NULL, NULL),
          (194002, 32101, 2, 'synthetic-order:1:2', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           1, 1, 32100, 32100, NULL, NULL, NULL),
          (194003, 32101, 3, 'synthetic-order:1:3', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           1, 1, 32100, 32100, NULL, NULL, NULL),
          (194004, 32101, 4, 'synthetic-order:1:4', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           1, 1, 32100, 32100, NULL, NULL, NULL),
          (194011, 32102, 1, 'synthetic-order:2:1', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           7, 7, 32100, 32100, NULL, NULL, NULL),
          (194021, 32103, 1, 'synthetic-order:3:1', 'SYNTHETIC NONRELIGIOUS STRUCTURAL TEST ROW',
           2, 2, 32100, 32100, NULL, NULL, NULL);

        INSERT INTO quran_pos_tags (code, arabic_label, english_label, category, sort_order)
        VALUES
          ('SYNO', 'نوع ترتيب هيكلي غير قرآني', 'Synthetic ordering', 'noun', 32101),
          ('SYNT', 'نوع تعادل هيكلي غير قرآني', 'Synthetic ordering tie', 'noun', 32102);

        INSERT INTO quran_lemmas
          (id, lemma_text, lemma_buckwalter, root_id, words_count, first_word_order_in_mushaf)
        VALUES
          (194101, 'أ-ترتيب-هيكلي', 'synthetic-order-a', NULL, 6, 194301),
          (194102, 'ب-ترتيب-هيكلي', 'synthetic-order-b', NULL, 5, 194311),
          (194103, 'ج-ترتيب-هيكلي', 'synthetic-order-c', NULL, 4, 194321),
          (194111, 'د-ترتيب-هيكلي', 'synthetic-tie-d', NULL, 2, 194341),
          (194112, 'ه-ترتيب-هيكلي', 'synthetic-tie-e', NULL, 2, 194331),
          (194113, 'و-ترتيب-هيكلي', 'synthetic-tie-f', NULL, 2, 194351);

        INSERT INTO quran_words
          (id, location, ayah_id, surah_number, ayah_number, word_number, page_number,
           line_number, line_word_order, qpc_glyph, text_uthmani, text_uthmani_simple,
           text_imlaei_simple, word_key_imlaei_simple, is_ayah_marker,
           unique_tashkeel_word_id, unique_simple_word_id)
        VALUES
          -- Group A: 6 occurrences over 2 ayahs in 2 surahs.
          (194301, 'synthetic-order:194301', 194001, 32101, 1, 1, 32100, 1, 1,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),
          (194302, 'synthetic-order:194302', 194001, 32101, 1, 2, 32100, 1, 2,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),
          (194303, 'synthetic-order:194303', 194001, 32101, 1, 3, 32100, 1, 3,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),
          (194304, 'synthetic-order:194304', 194011, 32102, 1, 1, 32100, 1, 4,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),
          (194305, 'synthetic-order:194305', 194011, 32102, 1, 2, 32100, 1, 5,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),
          (194306, 'synthetic-order:194306', 194011, 32102, 1, 3, 32100, 1, 6,
           'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', FALSE, NULL, NULL),

          -- Group B: 5 occurrences over 4 ayahs in 1 surah.
          (194311, 'synthetic-order:194311', 194001, 32101, 1, 4, 32100, 1, 7,
           'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', FALSE, NULL, NULL),
          (194312, 'synthetic-order:194312', 194001, 32101, 1, 5, 32100, 1, 8,
           'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', FALSE, NULL, NULL),
          (194313, 'synthetic-order:194313', 194002, 32101, 2, 1, 32100, 1, 9,
           'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', FALSE, NULL, NULL),
          (194314, 'synthetic-order:194314', 194003, 32101, 3, 1, 32100, 1, 10,
           'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', FALSE, NULL, NULL),
          (194315, 'synthetic-order:194315', 194004, 32101, 4, 1, 32100, 1, 11,
           'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', FALSE, NULL, NULL),

          -- Group C: 4 occurrences over 3 ayahs in 3 surahs.
          (194321, 'synthetic-order:194321', 194001, 32101, 1, 6, 32100, 1, 12,
           'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', FALSE, NULL, NULL),
          (194322, 'synthetic-order:194322', 194011, 32102, 1, 4, 32100, 1, 13,
           'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', FALSE, NULL, NULL),
          (194323, 'synthetic-order:194323', 194021, 32103, 1, 1, 32100, 1, 14,
           'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', FALSE, NULL, NULL),
          (194324, 'synthetic-order:194324', 194021, 32103, 1, 2, 32100, 1, 15,
           'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', FALSE, NULL, NULL),

          -- Tie group E, seeded before D and F so the lowest word id does NOT belong to the lowest id.
          (194331, 'synthetic-order:194331', 194001, 32101, 1, 7, 32100, 1, 16,
           'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', FALSE, NULL, NULL),
          (194332, 'synthetic-order:194332', 194011, 32102, 1, 5, 32100, 1, 17,
           'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', FALSE, NULL, NULL),

          -- Tie group D.
          (194341, 'synthetic-order:194341', 194001, 32101, 1, 8, 32100, 1, 18,
           'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', FALSE, NULL, NULL),
          (194342, 'synthetic-order:194342', 194011, 32102, 1, 6, 32100, 1, 19,
           'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', FALSE, NULL, NULL),

          -- Tie group F.
          (194351, 'synthetic-order:194351', 194001, 32101, 1, 9, 32100, 1, 20,
           'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', FALSE, NULL, NULL),
          (194352, 'synthetic-order:194352', 194011, 32102, 1, 7, 32100, 1, 21,
           'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', FALSE, NULL, NULL);

        INSERT INTO quran_words_unique_tashkeel
          (id, text_uthmani, text_uthmani_simple, text_imlaei_simple, occurrences_count,
           ayahs_count, surahs_count, first_quran_word_id, first_location, first_surah_number,
           first_ayah_number, first_word_order_in_mushaf, first_page_number, first_line_number)
        VALUES
          (194201, 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 'SYNTH_ORDER_A', 6, 2, 2,
           194301, 'synthetic-order:194301', 32101, 1, 194301, 32100, 1),
          (194202, 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 'SYNTH_ORDER_B', 5, 4, 1,
           194311, 'synthetic-order:194311', 32101, 1, 194311, 32100, 1),
          (194203, 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 'SYNTH_ORDER_C', 4, 3, 3,
           194321, 'synthetic-order:194321', 32101, 1, 194321, 32100, 1),
          (194211, 'SYNTH_TIE_D', 'SYNTH_TIE_D', 'SYNTH_TIE_D', 2, 2, 2,
           194341, 'synthetic-order:194341', 32101, 1, 194341, 32100, 1),
          (194212, 'SYNTH_TIE_E', 'SYNTH_TIE_E', 'SYNTH_TIE_E', 2, 2, 2,
           194331, 'synthetic-order:194331', 32101, 1, 194331, 32100, 1),
          (194213, 'SYNTH_TIE_F', 'SYNTH_TIE_F', 'SYNTH_TIE_F', 2, 2, 2,
           194351, 'synthetic-order:194351', 32101, 1, 194351, 32100, 1);

        UPDATE quran_words SET unique_tashkeel_word_id = 194201 WHERE id IN (194301, 194302, 194303, 194304, 194305, 194306);
        UPDATE quran_words SET unique_tashkeel_word_id = 194202 WHERE id IN (194311, 194312, 194313, 194314, 194315);
        UPDATE quran_words SET unique_tashkeel_word_id = 194203 WHERE id IN (194321, 194322, 194323, 194324);
        UPDATE quran_words SET unique_tashkeel_word_id = 194211 WHERE id IN (194341, 194342);
        UPDATE quran_words SET unique_tashkeel_word_id = 194212 WHERE id IN (194331, 194332);
        UPDATE quran_words SET unique_tashkeel_word_id = 194213 WHERE id IN (194351, 194352);

        INSERT INTO quran_word_morphology
          (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id,
           is_verb, verb_tense, verb_voice, case_feature, head_features_json)
        VALUES
          (194301, 'synthetic-order:194301', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194302, 'synthetic-order:194302', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194303, 'synthetic-order:194303', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194304, 'synthetic-order:194304', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194305, 'synthetic-order:194305', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194306, 'synthetic-order:194306', 'SYNO', 1, NULL, 194101, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194311, 'synthetic-order:194311', 'SYNO', 1, NULL, 194102, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194312, 'synthetic-order:194312', 'SYNO', 1, NULL, 194102, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194313, 'synthetic-order:194313', 'SYNO', 1, NULL, 194102, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194314, 'synthetic-order:194314', 'SYNO', 1, NULL, 194102, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194315, 'synthetic-order:194315', 'SYNO', 1, NULL, 194102, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194321, 'synthetic-order:194321', 'SYNO', 1, NULL, 194103, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194322, 'synthetic-order:194322', 'SYNO', 1, NULL, 194103, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194323, 'synthetic-order:194323', 'SYNO', 1, NULL, 194103, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194324, 'synthetic-order:194324', 'SYNO', 1, NULL, 194103, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194331, 'synthetic-order:194331', 'SYNT', 1, NULL, 194112, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194332, 'synthetic-order:194332', 'SYNT', 1, NULL, 194112, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194341, 'synthetic-order:194341', 'SYNT', 1, NULL, 194111, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194342, 'synthetic-order:194342', 'SYNT', 1, NULL, 194111, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194351, 'synthetic-order:194351', 'SYNT', 1, NULL, 194113, NULL, FALSE, NULL, NULL, NULL, NULL),
          (194352, 'synthetic-order:194352', 'SYNT', 1, NULL, 194113, NULL, FALSE, NULL, NULL, NULL, NULL);
        """;
}
