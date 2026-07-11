using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesTableReadTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task RootsView_Groups_ByRootId_WithScopedCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Roots,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // Noun scope root 190700 (ك ل م) covers 3 occurrences (1903001 N, 1903002 PN, 1903003 ADJ);
        // مُثَل (1903011, N) carries no root and must be excluded.
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        var row = page.Items.Single().Should().BeOfType<RootTableRowDto>().Subject;
        row.RootId.Should().Be(190700);
        row.DisplayText.Should().Be("ك ل م");
        row.OccurrencesCount.Should().Be(3);
        row.AyahsCount.Should().Be(2);
        row.SurahsCount.Should().Be(1);
    }

    [Fact]
    public async Task StemsView_Groups_ByStemId_WithScopedCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Stems,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        var row = page.Items.Single().Should().BeOfType<StemTableRowDto>().Subject;
        row.StemId.Should().Be(190600);
        row.OccurrencesCount.Should().Be(3);
    }

    [Fact]
    public async Task LemmasView_Groups_ByLemmaId_WithScopedCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        page.TotalCount.Should().Be(1);
        var row = page.Items.Single().Should().BeOfType<LemmaTableRowDto>().Subject;
        row.LemmaId.Should().Be(190500);
        row.OccurrencesCount.Should().Be(3);
    }

    [Fact]
    public async Task GroupedViews_UseActiveSecondaryFilter_ToNarrowCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var pastOnly = await reader.GetTableRowsAsync(
            new WordTypeFilter("verb", null, null, "past", null),
            WordTypeTableView.Roots,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // Verb root 190701 (ع ل م) has 3 total occurrences, but tense=past narrows to 1 (1907001).
        pastOnly.TotalCount.Should().Be(1);
        var row = pastOnly.Items.Single().Should().BeOfType<RootTableRowDto>().Subject;
        row.RootId.Should().Be(190701);
        row.OccurrencesCount.Should().Be(1);
        row.AyahsCount.Should().Be(1);
        row.SurahsCount.Should().Be(1);
    }

    [Fact]
    public async Task GroupedViews_ExcludeNullDimension_ButOccurrenceSumsReconcileAgainstWordsView()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var wordsPage = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Words,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var lemmasPage = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // Noun scope: N/PN/ADJ (1903001-1903003, tashkeel 191001) share lemma_id 190500 (3 occurrences).
        // مُثَل (1903011, N) has a NULL lemma_id and must be excluded from the lemmas grouping, so the
        // words-view occurrence sum (4) must exceed the lemmas-view occurrence sum (3) by exactly the
        // null-lemma row's own OccurrencesCount (1) -- a non-zero, checkable reconciliation.
        var wordsOccurrenceSum = wordsPage.Items.Sum(row => ((WordTableRowDto)row).OccurrencesCount);
        var lemmasOccurrenceSum = lemmasPage.Items.Sum(row => ((LemmaTableRowDto)row).OccurrencesCount);
        var muthalRow = wordsPage.Items.Cast<WordTableRowDto>().Single(row => row.DisplayText == "مُثَل");

        wordsOccurrenceSum.Should().Be(4);
        lemmasOccurrenceSum.Should().Be(3);
        (wordsOccurrenceSum - lemmasOccurrenceSum).Should().Be(muthalRow.OccurrencesCount).And.NotBe(0);
    }

    [Fact]
    public async Task GroupedViews_Sort_ByMushafOrder_Deterministically()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // inl scope has two lemma groups: 190503 (الٓمٓ, first occurrence 1903005) and
        // 190505 (ص, first occurrence 1903012). MushafOrder must place the earlier Mushaf
        // occurrence first, proving the multi-group ORDER BY (not a single-row coincidence).
        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.MushafOrder,
            1,
            25,
            CancellationToken.None);

        page.Items.Select(row => ((LemmaTableRowDto)row).LemmaId).Should().Equal(190503, 190505);
    }

    [Fact]
    public async Task GroupedViews_Sort_ByAlpha_UsesArabicFoldAndCollation_Deterministically()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // Exercises the previously-uncovered Alpha branch: the norm_text fold expression, the
        // COLLATE "C" ordinal ORDER BY, and the conditional @foldFrom/@foldTo parameter binding.
        // ا (U+0627, لemma 190503 الٓمٓ) sorts before ص (U+0635, lemma 190505 ص) both raw and
        // folded (neither is a hamza-family character), so the order is deterministic.
        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Alpha,
            1,
            25,
            CancellationToken.None);

        page.TotalCount.Should().Be(2);
        page.Items.Select(row => ((LemmaTableRowDto)row).LemmaId).Should().Equal(190503, 190505);
    }

    [Fact]
    public async Task GroupedViews_Paginate_AfterGroupingAndCounting()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // inl/lemmas has two groups with an equal OccurrencesCount (1 each), so the default
        // Occurrences sort falls through to the first_word_order_in_mushaf tie-break: 1903005
        // (lemma 190503) precedes 1903012 (lemma 190505). pageSize=1 proves grouping/counting
        // happen before pagination and that page 2 continues the same deterministic order.
        var pageOne = await reader.GetTableRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Occurrences,
            1,
            1,
            CancellationToken.None);
        var pageTwo = await reader.GetTableRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Occurrences,
            2,
            1,
            CancellationToken.None);
        var pageThree = await reader.GetTableRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSort.Occurrences,
            3,
            1,
            CancellationToken.None);

        pageOne.TotalCount.Should().Be(2);
        pageTwo.TotalCount.Should().Be(2);
        ((LemmaTableRowDto)pageOne.Items.Single()).LemmaId.Should().Be(190503);
        ((LemmaTableRowDto)pageTwo.Items.Single()).LemmaId.Should().Be(190505);
        pageThree.TotalCount.Should().Be(2);
        pageThree.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task WordsView_ReturnsWordVariant_WithCompleteCompositeIdentity()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, "genitive", null, null),
            WordTypeTableView.Words,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        page.Items.Should().ContainSingle();
        var row = page.Items.Single().Should().BeOfType<WordTableRowDto>().Subject;
        row.Case.Should().Be("genitive");
        row.Tense.Should().BeNull();
        row.Voice.Should().BeNull();
        row.ContextCode.Should().Be("PN");
    }

    [Fact]
    public async Task Table_MissingTableView_DefaultsToWords()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeTableHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeTableQuery("noun", null, null, null, null, null, "occurrences", 1, 25),
            CancellationToken.None);

        var success = outcome.Should().BeOfType<GetWordTypeTableOutcome.Success>().Subject;
        success.Page.Items.Should().OnlyContain(row => row is WordTableRowDto);
    }

    [Fact]
    public async Task Table_UnknownTableView_ReturnsInvalidTableView()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeTableHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeTableQuery("noun", null, null, null, null, "bogus", "occurrences", 1, 25),
            CancellationToken.None);

        outcome.Should().BeOfType<GetWordTypeTableOutcome.InvalidTableView>();
    }

    [Fact]
    public async Task Table_TableViewWords_MatchesLegacyWordsEndpoint_PlusKindDiscriminator()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var legacy = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var table = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Words,
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        table.TotalCount.Should().Be(legacy.TotalCount);
        var tableWordRows = table.Items.Cast<WordTableRowDto>().ToList();
        tableWordRows.Select(row => row.TashkeelWordId).Should().Equal(legacy.Items.Select(row => row.TashkeelWordId));
        tableWordRows.Select(row => row.ContextCode).Should().Equal(legacy.Items.Select(row => row.ContextCode));
        tableWordRows.Select(row => row.OccurrencesCount).Should().Equal(legacy.Items.Select(row => row.OccurrencesCount));
    }

    [Fact]
    public void CacheKeys_TableView_ProducesDistinctKeys_PerView()
    {
        var filter = new WordTypeFilter("noun", null, null, null, null);

        var rootsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Roots, WordTypeSort.Occurrences, 1, 25);
        var stemsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Stems, WordTypeSort.Occurrences, 1, 25);
        var wordsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Words, WordTypeSort.Occurrences, 1, 25);

        rootsKey.Should().NotBe(stemsKey);
        rootsKey.Should().NotBe(wordsKey);
        stemsKey.Should().NotBe(wordsKey);
        rootsKey.Should().Contain(":view:roots:");
        stemsKey.Should().Contain(":view:stems:");
    }

    [Fact]
    public async Task CachedReader_TableRows_RootsAndStems_NeverCrossServe()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();
        var filter = new WordTypeFilter("noun", null, null, null, null);

        var rootsFirst = await reader.GetTableRowsAsync(filter, WordTypeTableView.Roots, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var rootsSecond = await reader.GetTableRowsAsync(filter, WordTypeTableView.Roots, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var stems = await reader.GetTableRowsAsync(filter, WordTypeTableView.Stems, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);

        // Same (filter, tableView, sort, page, pageSize) must hit the cache (identical reference);
        // a different tableView for the same filter must be a cache miss serving distinct content,
        // proving roots/stems never cross-serve through the shared IMemoryCache instance.
        rootsSecond.Should().BeSameAs(rootsFirst);
        stems.Should().NotBeSameAs(rootsFirst);
        rootsFirst.Items.Should().OnlyContain(row => row is RootTableRowDto);
        stems.Items.Should().OnlyContain(row => row is StemTableRowDto);
    }
}
