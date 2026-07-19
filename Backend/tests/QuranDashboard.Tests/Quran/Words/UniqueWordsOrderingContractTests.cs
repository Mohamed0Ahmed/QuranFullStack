using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

// The Unique Words ORDER BY contract (Feature 030, N8), proved against real PostgreSQL.
//
// The seeded content slice cannot prove this on its own: every seeded row carries
// occurrences_count == ayahs_count == surahs_count and first_word_order_in_mushaf == id, so a count
// arm reading the wrong column — or a tie resolved by Id instead of Mushaf order — is invisible there.
// Each test below therefore inserts its own rows inside a transaction that is ALWAYS rolled back (the
// await using disposal rolls back even when an assertion throws), so the shared fixture slice every
// other suite asserts on is left byte-identical.
//
// The inserted rows are explicitly synthetic structural placeholders: ASCII labels, no Arabic and no
// Quranic content of any kind. They exist only to give the ORDER BY distinguishable keys.
//
// Each test claims its own occurrences band (100s / 200s / 300s) and scopes the read with a count
// filter over that band. That isolates the assertion from the eight seeded rows, and — because the band
// is part of the list cache key — it stops the cached reader from cross-serving one test's rolled-back
// rows to another.
[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsOrderingContractTests(UniqueWordsTestFixture fixture)
{
    // A real quran_words row the synthetic identities can point their required FK at.
    private const int SeededFirstQuranWordId = 1001;
    private const string SyntheticLabelPrefix = "SYNTHETIC-ORDER-ROW-";
    private const string MushafOrderUniqueIndex = "IX_quran_words_unique_tashkeel_first_word_order_in_mushaf";

    // Distinct counts per column, and a Mushaf order that contradicts Id order, so each of the six
    // (column × direction) pairs has its OWN expected sequence: an arm that reads a neighbouring count
    // column, or that reverses, lands on a different sequence and fails.
    //   Id      occ  ayahs  surahs  mushaf
    //   900001  103  11     7       900300
    //   900002  102  13     5       900100
    //   900003  101  12     6       900200
    private static readonly SyntheticWord[] DistinctCountRows =
    [
        new(900001, Occurrences: 103, Ayahs: 11, Surahs: 7, MushafOrder: 900300),
        new(900002, Occurrences: 102, Ayahs: 13, Surahs: 5, MushafOrder: 900100),
        new(900003, Occurrences: 101, Ayahs: 12, Surahs: 6, MushafOrder: 900200),
    ];

    // All three counts tied, Mushaf order deliberately NOT in Id order: an Id-first tie-break would
    // read 910001, 910002, 910003 instead.
    private static readonly SyntheticWord[] MushafTieRows =
    [
        new(910001, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910300),
        new(910002, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910100),
        new(910003, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910200),
    ];

    // Counts AND Mushaf order all tied, so only Id can decide. Listed — and inserted — in DESCENDING
    // Id so the heap order contradicts the expectation: an ORDER BY that stopped at Mushaf order would
    // leave PostgreSQL free to hand these back in storage order, which is the reverse of what we assert.
    private static readonly SyntheticWord[] IdentityTieRows =
    [
        new(920005, Occurrences: 305, Ayahs: 5, Surahs: 5, MushafOrder: 920100),
        new(920004, Occurrences: 305, Ayahs: 5, Surahs: 5, MushafOrder: 920100),
        new(920003, Occurrences: 305, Ayahs: 5, Surahs: 5, MushafOrder: 920100),
        new(920002, Occurrences: 305, Ayahs: 5, Surahs: 5, MushafOrder: 920100),
        new(920001, Occurrences: 305, Ayahs: 5, Surahs: 5, MushafOrder: 920100),
    ];

    [Theory]
    [InlineData("occurrences", new[] { 900001, 900002, 900003 })]
    [InlineData("occurrences-asc", new[] { 900003, 900002, 900001 })]
    [InlineData("ayahs", new[] { 900002, 900003, 900001 })]
    [InlineData("ayahs-asc", new[] { 900001, 900003, 900002 })]
    [InlineData("surahs", new[] { 900001, 900003, 900002 })]
    [InlineData("surahs-asc", new[] { 900002, 900003, 900001 })]
    public async Task Each_count_column_orders_by_its_own_count_in_both_directions(string sort, int[] expectedIds)
    {
        var ids = await OrderedIdsOverSyntheticRowsAsync(DistinctCountRows, occurrencesBand: 100, sort);

        ids.Should().Equal(expectedIds);
    }

    [Theory]
    [InlineData("occurrences")]
    [InlineData("occurrences-asc")]
    [InlineData("ayahs")]
    [InlineData("ayahs-asc")]
    [InlineData("surahs")]
    [InlineData("surahs-asc")]
    public async Task Equal_counts_order_by_mushaf_order_in_both_directions(string sort)
    {
        var ids = await OrderedIdsOverSyntheticRowsAsync(MushafTieRows, occurrencesBand: 200, sort);

        // Reversing the primary must never reshuffle a tie group, or a page boundary could drop or
        // repeat a row — so the SAME sequence is expected ascending and descending.
        ids.Should().Equal(910002, 910003, 910001);
    }

    [Theory]
    [InlineData("occurrences")]
    [InlineData("occurrences-asc")]
    [InlineData("ayahs")]
    [InlineData("ayahs-asc")]
    [InlineData("surahs")]
    [InlineData("surahs-asc")]
    public async Task Equal_counts_and_equal_mushaf_order_fall_through_to_id_in_both_directions(string sort)
    {
        // first_word_order_in_mushaf carries a UNIQUE index, so production data can never reach the last
        // rung of the chain. Dropping the index inside the rolled-back transaction is the only way to
        // exercise it — and it must be exercised, because it is the reader's sole guarantee that a
        // LINQ-to-SQL ORDER BY (which has no stable-sort net) returns a deterministic page.
        var ids = await OrderedIdsOverSyntheticRowsAsync(
            IdentityTieRows,
            occurrencesBand: 300,
            sort,
            dropMushafOrderUniqueIndex: true);

        ids.Should().Equal(920001, 920002, 920003, 920004, 920005);
    }

    private async Task<IReadOnlyList<int>> OrderedIdsOverSyntheticRowsAsync(
        IReadOnlyList<SyntheticWord> rows,
        int occurrencesBand,
        string sort,
        bool dropMushafOrderUniqueIndex = false)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();

        // Never committed: disposal rolls the inserts (and the index drop) back on every exit path.
        await using var transaction = await db.Database.BeginTransactionAsync();

        if (dropMushafOrderUniqueIndex)
        {
            await db.Database.ExecuteSqlRawAsync($"""DROP INDEX "{MushafOrderUniqueIndex}" """);
        }

        await InsertSyntheticTashkeelWordsAsync(db, rows);

        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();
        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(
                "tashkeel",
                null,
                sort,
                1,
                50,
                UniqueWordsCountFilter.FromRaw(
                    occMin: occurrencesBand,
                    occMax: occurrencesBand + 99,
                    ayahsMin: null, ayahsMax: null,
                    surahsMin: null, surahsMax: null)),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(rows.Count, "the count band must scope the read to the synthetic rows alone");

        return [.. page.Items.Select(item => item.Id)];
    }

    // The reader resolves its rows through the SAME scoped DbContext, so it reads inside this
    // transaction and sees the uncommitted rows. search_text_normalized is a stored generated column and
    // is deliberately not written here.
    private static Task InsertSyntheticTashkeelWordsAsync(QuranDashboardDbContext db, IReadOnlyList<SyntheticWord> rows)
    {
        var values = string.Join(
            ",\n",
            rows.Select(row =>
                $"({row.Id}, '{SyntheticLabelPrefix}{row.Id}', '{SyntheticLabelPrefix}{row.Id}', '{SyntheticLabelPrefix}{row.Id}', "
                + $"{row.Occurrences}, {row.Ayahs}, {row.Surahs}, {SeededFirstQuranWordId}, 'synthetic:{row.Id}', 1, 1, "
                + $"{row.MushafOrder}, 1, 1)"));

        // Composed into a local first: every value above is a test-owned int or constant label, so
        // ExecuteSqlRawAsync's interpolation warning has nothing to bite on here.
        var sql = $"""
            INSERT INTO quran_words_unique_tashkeel
              (id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
               occurrences_count, ayahs_count, surahs_count,
               first_quran_word_id, first_location, first_surah_number, first_ayah_number,
               first_word_order_in_mushaf, first_page_number, first_line_number)
            VALUES
            {values}
            """;

        return db.Database.ExecuteSqlRawAsync(sql);
    }

    private sealed record SyntheticWord(int Id, int Occurrences, short Ayahs, short Surahs, int MushafOrder);
}
