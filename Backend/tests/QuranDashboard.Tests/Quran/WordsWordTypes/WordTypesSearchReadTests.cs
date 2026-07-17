using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Feature 026 US1: Word Types search over the clean word-identity text only. The predicate lives on the
// shared BaseRowsSql occurrence base, so the words view, all three grouped views, and their TotalCounts
// narrow together; grouped-detail reads never take a search term.
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesSearchReadTests(WordTypesTestFixture fixture)
{
    // (a) Search narrows the words view by word-identity text, tashkeel-insensitively. "كَلِم" (diacritized)
    // matches only the كَلِمَة identity (191001) across its three noun contexts, never the مُثَل identity.
    [Fact]
    public async Task Search_NarrowsWordsView_ByIdentityText_TashkeelInsensitive()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var unfiltered = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var searched = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "كَلِم"),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        unfiltered.TotalCount.Should().Be(4);
        searched.TotalCount.Should().Be(3);
        searched.Items.Should().OnlyContain(row => row.DisplayText == "كَلِمَة");
        searched.Items.Select(row => row.ContextCode).Should().BeEquivalentTo(["N", "PN", "ADJ"]);
    }

    // (b) The three grouped views AND their TotalCounts reflect the searched base. "كلم" keeps the
    // root/lemma/stem-bearing كَلِمَة identity (each grouped view = 1); "مثل" keeps only the rootless مُثَل
    // identity, so the words view has one row but every grouped view collapses to zero.
    [Theory]
    [InlineData(WordTypeTableView.Roots)]
    [InlineData(WordTypeTableView.Lemmas)]
    [InlineData(WordTypeTableView.Stems)]
    public async Task Search_NarrowsGroupedViewsAndTotals_ToTheSearchedBase(WordTypeTableView view)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var bearing = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "كلم"),
            view, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var rootless = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل"),
            view, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var rootlessWords = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل"),
            WordTypeTableView.Words, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        bearing.TotalCount.Should().Be(1);
        bearing.Items.Should().HaveCount(1);
        rootless.TotalCount.Should().Be(0);
        rootless.Items.Should().BeEmpty();
        rootlessWords.TotalCount.Should().Be(1);
    }

    // (c) A term that is only present in dimension display text — the spaced root text "ك ل م", which is
    // never a word identity — matches nothing, even though that root is in scope. Proves identity-only.
    [Fact]
    public async Task Search_ByDimensionDisplayText_MatchesNothing()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var byRootText = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "ك ل م"),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        byRootText.TotalCount.Should().Be(0);
        byRootText.Items.Should().BeEmpty();
    }

    // (d) Normalization equivalence with Unique Words search: the shared normalizer collapses diacritic
    // variants to the same skeleton in BOTH explorers. Every variant of the علم skeleton returns the same
    // Word Types verb rows as the bare form, and the same Unique Words tashkeel matches as the bare form.
    [Theory]
    [InlineData("عَلِمَ")]
    [InlineData("عُلِمَ")]
    public async Task Search_NormalizesIdenticallyToUniqueWords(string diacritizedTerm)
    {
        await using var scope = fixture.CreateScope();
        var wordTypes = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var uniqueWords = scope.ServiceProvider.GetRequiredService<IUniqueWordsReader>();

        var diacritizedRows = await wordTypes.GetRowsAsync(
            new WordTypeFilter("verb", null, null, null, null, diacritizedTerm),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var bareRows = await wordTypes.GetRowsAsync(
            new WordTypeFilter("verb", null, null, null, null, "علم"),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        diacritizedRows.Items.Select(row => (row.TashkeelWordId, row.ContextCode))
            .Should().BeEquivalentTo(bareRows.Items.Select(row => (row.TashkeelWordId, row.ContextCode)));
        diacritizedRows.Items.Should().NotBeEmpty();

        var diacritizedUnique = await uniqueWords.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel, diacritizedTerm, UniqueWordSortSpec.Natural(UniqueWordSortColumn.Occurrences), UniqueWordsCountFilter.None, UniqueWordsAssociationFilter.None, 1, 1000, CancellationToken.None);
        var bareUnique = await uniqueWords.GetUniqueWordsPageAsync(
            UniqueWordKind.Tashkeel, "علم", UniqueWordSortSpec.Natural(UniqueWordSortColumn.Occurrences), UniqueWordsCountFilter.None, UniqueWordsAssociationFilter.None, 1, 1000, CancellationToken.None);

        diacritizedUnique.Items.Select(item => item.Id)
            .Should().BeEquivalentTo(bareUnique.Items.Select(item => item.Id));
        bareUnique.Items.Should().NotBeEmpty();
    }

    // (g) Grouped-detail reads take no search term: a search set on the grouped selection's filter is
    // ignored, so the member-word page is identical whether or not one is present.
    [Fact]
    public async Task GroupedDetailReads_IgnoreSearch()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var withoutSearch = new WordTypeGroupedSelection(
            WordTypeGroupedDimensionKind.Root, 190700, new WordTypeFilter("noun", null, null, null, null));
        var withNonMatchingSearch = withoutSearch with
        {
            Filter = withoutSearch.Filter with { Search = "لا-تطابق" },
        };

        var baseline = await reader.GetGroupedMemberWordsAsync(withoutSearch, 1, 100, CancellationToken.None);
        var withSearch = await reader.GetGroupedMemberWordsAsync(withNonMatchingSearch, 1, 100, CancellationToken.None);

        baseline!.TotalCount.Should().Be(3);
        withSearch!.TotalCount.Should().Be(baseline.TotalCount);
        withSearch.Items.Should().HaveCount(baseline.Items.Count);
    }
}
