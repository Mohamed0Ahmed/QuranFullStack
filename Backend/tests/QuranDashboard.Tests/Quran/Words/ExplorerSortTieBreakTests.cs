using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Roots;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Tests.Quran.Words;

// Tie-break half of the ordering contract (Feature 030, N8). Two DIFFERENT rules are pinned: count
// columns tie-break on Mushaf order then Id; alpha ties on Id ALONE (no Mushaf tie-break) — that is
// what keeps every pre-existing sort=alpha link byte-identical. Each chain must be identical in BOTH
// directions: reversing a column must never reshuffle its ties, or a page boundary could drop or
// repeat a row. Rows are explicitly synthetic and carry no Quranic content.
public sealed class ExplorerSortTieBreakTests
{
    // Ids ascend while Mushaf order does NOT, so "tie-broke on Id" and "tie-broke on Mushaf order"
    // produce different sequences and the assertions below can tell the two apart.
    private const int TiedCount = 5;
    private const int HighestCount = 9;
    private const string TiedText = "synthetic-tie";
    private const string LastText = "synthetic-zzz";

    private static readonly (int Id, string Text, int Count, int MushafOrder)[] TieRows =
    [
        (1, TiedText, TiedCount, 300),
        (2, TiedText, TiedCount, 100),
        (3, TiedText, TiedCount, 200),
        (4, LastText, HighestCount, 400),
    ];

    // Count DESC puts the distinct high count first; the tie group then follows in Mushaf order.
    private static readonly int[] CountDescendingIds = [4, 2, 3, 1];

    // Count ASC flips only the PRIMARY: the tie group keeps the very same Mushaf-ascending order.
    private static readonly int[] CountAscendingIds = [2, 3, 1, 4];

    // Alpha ASC: the tie group orders by Id alone. Were Mushaf order in this chain, it would read
    // 2, 3, 1 instead — that difference is the whole point of this expectation.
    private static readonly int[] AlphaAscendingIds = [1, 2, 3, 4];

    // Alpha DESC flips only the PRIMARY: the tie group still reads by Id ascending.
    private static readonly int[] AlphaDescendingIds = [4, 1, 2, 3];

    [Theory]
    [InlineData(RootSortColumn.Occurrences, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.Ayahs, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.Surahs, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.SimpleWords, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.TashkeelWords, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.Lemmas, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.Stems, WordSortDirection.Descending)]
    [InlineData(RootSortColumn.Occurrences, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.Ayahs, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.Surahs, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.SimpleWords, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.TashkeelWords, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.Lemmas, WordSortDirection.Ascending)]
    [InlineData(RootSortColumn.Stems, WordSortDirection.Ascending)]
    public void Roots_count_columns_tie_break_on_mushaf_order_then_id_in_both_directions(
        RootSortColumn column,
        WordSortDirection direction)
    {
        // Every count on the row carries the same value, so one row set proves every count column.
        var ordered = RootsListDerivation.FilterAndSort(
            RootRows(),
            RootsCountFilter.None,
            search: null,
            new RootSortSpec(column, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Descending ? CountDescendingIds : CountAscendingIds);
    }

    [Theory]
    [InlineData(WordSortDirection.Ascending)]
    [InlineData(WordSortDirection.Descending)]
    public void Roots_alpha_ties_break_on_id_alone_without_a_mushaf_tie_break(WordSortDirection direction)
    {
        var ordered = RootsListDerivation.FilterAndSort(
            RootRows(),
            RootsCountFilter.None,
            search: null,
            new RootSortSpec(RootSortColumn.Alpha, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Ascending ? AlphaAscendingIds : AlphaDescendingIds,
            "alpha must keep the exact chain existing sort=alpha links already return");
    }

    [Theory]
    [InlineData(WordSortDirection.Ascending)]
    [InlineData(WordSortDirection.Descending)]
    public void Roots_mushaf_order_stays_ascending_whatever_direction_the_pair_carries(WordSortDirection direction)
    {
        // mushaf-order is ascending-only by contract; the parser rejects any suffix, so the reader
        // must stay on the release order whatever direction the pair happens to carry.
        var ordered = RootsListDerivation.FilterAndSort(
            RootRows(),
            RootsCountFilter.None,
            search: null,
            new RootSortSpec(RootSortColumn.MushafOrder, direction));

        ordered.Select(row => row.Id).Should().Equal(2, 3, 1, 4);
    }

    [Theory]
    [InlineData(LemmaSortColumn.Occurrences, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.Occurrences, WordSortDirection.Ascending)]
    [InlineData(LemmaSortColumn.Ayahs, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.Ayahs, WordSortDirection.Ascending)]
    [InlineData(LemmaSortColumn.Surahs, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.Surahs, WordSortDirection.Ascending)]
    [InlineData(LemmaSortColumn.SimpleWords, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.SimpleWords, WordSortDirection.Ascending)]
    [InlineData(LemmaSortColumn.TashkeelWords, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.TashkeelWords, WordSortDirection.Ascending)]
    [InlineData(LemmaSortColumn.Stems, WordSortDirection.Descending)]
    [InlineData(LemmaSortColumn.Stems, WordSortDirection.Ascending)]
    public void Lemmas_count_columns_tie_break_on_mushaf_order_then_id_in_both_directions(
        LemmaSortColumn column,
        WordSortDirection direction)
    {
        var ordered = LemmasListDerivation.FilterAndSort(
            LemmaRows(),
            LemmasCountFilter.None,
            LemmasAssociationFilter.None,
            search: null,
            new LemmaSortSpec(column, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Descending ? CountDescendingIds : CountAscendingIds);
    }

    [Theory]
    [InlineData(WordSortDirection.Ascending)]
    [InlineData(WordSortDirection.Descending)]
    public void Lemmas_alpha_ties_break_on_id_alone_without_a_mushaf_tie_break(WordSortDirection direction)
    {
        var ordered = LemmasListDerivation.FilterAndSort(
            LemmaRows(),
            LemmasCountFilter.None,
            LemmasAssociationFilter.None,
            search: null,
            new LemmaSortSpec(LemmaSortColumn.Alpha, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Ascending ? AlphaAscendingIds : AlphaDescendingIds);
    }

    [Theory]
    [InlineData(StemSortColumn.Occurrences, WordSortDirection.Descending)]
    [InlineData(StemSortColumn.Occurrences, WordSortDirection.Ascending)]
    [InlineData(StemSortColumn.Ayahs, WordSortDirection.Descending)]
    [InlineData(StemSortColumn.Ayahs, WordSortDirection.Ascending)]
    [InlineData(StemSortColumn.Surahs, WordSortDirection.Descending)]
    [InlineData(StemSortColumn.Surahs, WordSortDirection.Ascending)]
    [InlineData(StemSortColumn.SimpleWords, WordSortDirection.Descending)]
    [InlineData(StemSortColumn.SimpleWords, WordSortDirection.Ascending)]
    [InlineData(StemSortColumn.TashkeelWords, WordSortDirection.Descending)]
    [InlineData(StemSortColumn.TashkeelWords, WordSortDirection.Ascending)]
    public void Stems_count_columns_tie_break_on_mushaf_order_then_id_in_both_directions(
        StemSortColumn column,
        WordSortDirection direction)
    {
        var ordered = StemsListDerivation.FilterAndSort(
            StemRows(),
            StemsCountFilter.None,
            StemsAssociationFilter.None,
            search: null,
            new StemSortSpec(column, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Descending ? CountDescendingIds : CountAscendingIds);
    }

    [Theory]
    [InlineData(WordSortDirection.Ascending)]
    [InlineData(WordSortDirection.Descending)]
    public void Stems_alpha_ties_break_on_id_alone_without_a_mushaf_tie_break(WordSortDirection direction)
    {
        var ordered = StemsListDerivation.FilterAndSort(
            StemRows(),
            StemsCountFilter.None,
            StemsAssociationFilter.None,
            search: null,
            new StemSortSpec(StemSortColumn.Alpha, direction));

        ordered.Select(row => row.Id).Should().Equal(
            direction == WordSortDirection.Ascending ? AlphaAscendingIds : AlphaDescendingIds);
    }

    private static IReadOnlyList<RootSummaryRow> RootRows() =>
    [
        .. TieRows.Select(row => new RootSummaryRow(
            row.Id,
            row.Text,
            row.Text,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.MushafOrder)),
    ];

    private static IReadOnlyList<LemmaSummaryRow> LemmaRows() =>
    [
        .. TieRows.Select(row => new LemmaSummaryRow(
            row.Id,
            row.Text,
            null,
            row.Text,
            null,
            null,
            null,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            "synthetic:0:0",
            row.MushafOrder,
            [])),
    ];

    private static IReadOnlyList<StemSummaryRow> StemRows() =>
    [
        .. TieRows.Select(row => new StemSummaryRow(
            row.Id,
            row.Text,
            row.Text,
            null,
            null,
            null,
            null,
            null,
            null,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            row.Count,
            "synthetic:0:0",
            row.MushafOrder,
            [])),
    ];
}
