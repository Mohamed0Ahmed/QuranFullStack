using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsOrderingContractTests(UniqueWordsTestFixture fixture)
{
    private const int SeededFirstQuranWordId = 1001;
    private const string SyntheticLabelPrefix = "SYNTHETIC-ORDER-ROW-";
    private const string MushafOrderUniqueIndex = "IX_quran_words_unique_tashkeel_first_word_order_in_mushaf";

    private static readonly SyntheticWord[] DistinctCountRows =
    [
        new(900001, Occurrences: 103, Ayahs: 11, Surahs: 7, MushafOrder: 900300),
        new(900002, Occurrences: 102, Ayahs: 13, Surahs: 5, MushafOrder: 900100),
        new(900003, Occurrences: 101, Ayahs: 12, Surahs: 6, MushafOrder: 900200),
    ];

    private static readonly SyntheticWord[] MushafTieRows =
    [
        new(910001, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910300),
        new(910002, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910100),
        new(910003, Occurrences: 205, Ayahs: 5, Surahs: 5, MushafOrder: 910200),
    ];

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

    private static Task InsertSyntheticTashkeelWordsAsync(QuranDashboardDbContext db, IReadOnlyList<SyntheticWord> rows)
    {
        var values = string.Join(
            ",\n",
            rows.Select(row =>
                $"({row.Id}, '{SyntheticLabelPrefix}{row.Id}', '{SyntheticLabelPrefix}{row.Id}', '{SyntheticLabelPrefix}{row.Id}', "
                + $"{row.Occurrences}, {row.Ayahs}, {row.Surahs}, {SeededFirstQuranWordId}, 'synthetic:{row.Id}', 1, 1, "
                + $"{row.MushafOrder}, 1, 1)"));

        // Safe to interpolate: every value is a test-owned int or constant label, never user input (injection).
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
