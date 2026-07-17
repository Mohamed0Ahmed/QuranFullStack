using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

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
            WordTypeSortSpec.Default,
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
            WordTypeSortSpec.Default,
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
            WordTypeSortSpec.Default,
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
            WordTypeSortSpec.Default,
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
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);
        var lemmasPage = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Default,
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
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await WordTypesSyntheticStructuralData.BeginSeededTransactionAsync(dbContext);
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", WordTypesSyntheticStructuralData.NounChildCode, null, null, null),
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Natural(WordTypeSortColumn.MushafOrder),
            1,
            25,
            CancellationToken.None);

        page.Items.Select(row => ((LemmaTableRowDto)row).LemmaId).Should().Equal(
            WordTypesSyntheticStructuralData.MushafFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId,
            WordTypesSyntheticStructuralData.FoldFirstLemmaId);
    }

    [Fact]
    public async Task GroupedViews_Sort_ByAlpha_UsesArabicFoldAndCollation_Deterministically()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await WordTypesSyntheticStructuralData.BeginSeededTransactionAsync(dbContext);
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var filter = new WordTypeFilter(
            "noun", WordTypesSyntheticStructuralData.NounChildCode, null, null, null);

        var alphaPage = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Natural(WordTypeSortColumn.Alpha),
            1,
            25,
            CancellationToken.None);
        var mushafPage = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Natural(WordTypeSortColumn.MushafOrder),
            1,
            25,
            CancellationToken.None);

        var alphaIds = alphaPage.Items.Cast<LemmaTableRowDto>().Select(row => row.LemmaId).ToArray();
        var rawOrdinalIds = alphaPage.Items.Cast<LemmaTableRowDto>()
            .OrderBy(row => row.DisplayText, StringComparer.Ordinal)
            .Select(row => row.LemmaId)
            .ToArray();
        var mushafIds = mushafPage.Items.Cast<LemmaTableRowDto>().Select(row => row.LemmaId).ToArray();

        alphaPage.TotalCount.Should().Be(3);
        rawOrdinalIds.Should().Equal(
            WordTypesSyntheticStructuralData.FoldSecondLemmaId,
            WordTypesSyntheticStructuralData.FoldFirstLemmaId,
            WordTypesSyntheticStructuralData.MushafFirstLemmaId);
        alphaIds.Should().Equal(
            WordTypesSyntheticStructuralData.FoldFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId,
            WordTypesSyntheticStructuralData.MushafFirstLemmaId);
        mushafIds.Should().Equal(
            WordTypesSyntheticStructuralData.MushafFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId,
            WordTypesSyntheticStructuralData.FoldFirstLemmaId);
    }

    // Alpha DESC must fold too. Two guarantees are proven here at once:
    //  (1) It RUNS. The folded `norm_text` column and the @foldFrom/@foldTo parameters are both gated
    //      on NeedsFold; if that gate missed the descending arm, Postgres would reject the missing
    //      column (or Npgsql the unbound parameter) at runtime rather than return rows.
    //  (2) It FOLDS. Unfolded, the two fold-colliding lemmas are DISTINCT texts ('أ-هيكلي' < 'إ-هيكلي')
    //      and would SWAP between ascending and descending. Folded, they collide onto one norm_text and
    //      the dimension_id tie-break pins them in the same order in BOTH directions — so the pair NOT
    //      flipping is exactly the evidence that the descending arm folds.
    [Fact]
    public async Task GroupedViews_Sort_ByAlphaDescending_AlsoFolds_AndKeepsTieOrder()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await WordTypesSyntheticStructuralData.BeginSeededTransactionAsync(dbContext);
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var filter = new WordTypeFilter(
            "noun", WordTypesSyntheticStructuralData.NounChildCode, null, null, null);

        var ascendingPage = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            new WordTypeSortSpec(WordTypeSortColumn.Alpha, WordSortDirection.Ascending),
            1,
            25,
            CancellationToken.None);
        var descendingPage = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            new WordTypeSortSpec(WordTypeSortColumn.Alpha, WordSortDirection.Descending),
            1,
            25,
            CancellationToken.None);

        var ascendingIds = ascendingPage.Items.Cast<LemmaTableRowDto>().Select(row => row.LemmaId).ToArray();
        var descendingIds = descendingPage.Items.Cast<LemmaTableRowDto>().Select(row => row.LemmaId).ToArray();

        descendingPage.TotalCount.Should().Be(3);
        descendingIds.Should().Equal(
            WordTypesSyntheticStructuralData.MushafFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId);

        // The fold-collided pair holds its dimension_id order under BOTH directions.
        ascendingIds.Should().ContainInOrder(
            WordTypesSyntheticStructuralData.FoldFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId);
        descendingIds.Should().ContainInOrder(
            WordTypesSyntheticStructuralData.FoldFirstLemmaId,
            WordTypesSyntheticStructuralData.FoldSecondLemmaId);

        // Reversing a column must never reshuffle its ties, so DESC is NOT a mirror of ASC.
        descendingIds.Should().NotEqual(ascendingIds.Reverse());
    }

    [Fact]
    public async Task GroupedViews_Paginate_AfterGroupingAndCounting()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await WordTypesSyntheticStructuralData.BeginSeededTransactionAsync(dbContext);
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();
        var filter = new WordTypeFilter(
            "noun", WordTypesSyntheticStructuralData.NounChildCode, null, null, null);

        var pageOne = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Default,
            1,
            1,
            CancellationToken.None);
        var pageTwo = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Default,
            2,
            1,
            CancellationToken.None);
        var pageThree = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Default,
            3,
            1,
            CancellationToken.None);
        var pageFour = await reader.GetTableRowsAsync(
            filter,
            WordTypeTableView.Lemmas,
            WordTypeSortSpec.Default,
            4,
            1,
            CancellationToken.None);

        new[] { pageOne, pageTwo, pageThree, pageFour }
            .Should().OnlyContain(page => page.TotalCount == 3);
        new[] { pageOne, pageTwo, pageThree }
            .Select(page => ((LemmaTableRowDto)page.Items.Single()).LemmaId)
            .Should().Equal(
                WordTypesSyntheticStructuralData.MushafFirstLemmaId,
                WordTypesSyntheticStructuralData.FoldSecondLemmaId,
                WordTypesSyntheticStructuralData.FoldFirstLemmaId);
        pageFour.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task WordsView_ReturnsWordVariant_WithCompleteCompositeIdentity()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var page = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, "genitive", null, null),
            WordTypeTableView.Words,
            WordTypeSortSpec.Default,
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
            new GetWordTypeTableQuery("noun", null, null, null, null, null, null, "occurrences", 1, 25),
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
            new GetWordTypeTableQuery("noun", null, null, null, null, null, "bogus", "occurrences", 1, 25),
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
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);
        var table = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Words,
            WordTypeSortSpec.Default,
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

        var rootsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 25);
        var stemsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Stems, WordTypeSortSpec.Default, 1, 25);
        var wordsKey = WordTypesCacheKeys.Table(filter, WordTypeTableView.Words, WordTypeSortSpec.Default, 1, 25);

        rootsKey.Should().NotBe(stemsKey);
        rootsKey.Should().NotBe(wordsKey);
        stemsKey.Should().NotBe(wordsKey);
        rootsKey.Should().Contain(":view:roots:");
        stemsKey.Should().Contain(":view:stems:");
    }

    // Every canonical token must key its own entry, and must appear in the key VERBATIM — the key
    // template is unchanged by N8, only the value set is wider.
    [Fact]
    public void CacheKeys_ProduceDistinctKeys_PerCanonicalSortToken()
    {
        var filter = new WordTypeFilter("noun", null, null, null, null);
        var keysByToken = CanonicalSortSpecs().ToDictionary(
            group => group.Key,
            group => (
                Rows: WordTypesCacheKeys.Rows(filter, group.First(), 1, 25),
                Table: WordTypesCacheKeys.Table(filter, WordTypeTableView.Words, group.First(), 1, 25)));

        keysByToken.Values.Select(pair => pair.Rows).Should().OnlyHaveUniqueItems();
        keysByToken.Values.Select(pair => pair.Table).Should().OnlyHaveUniqueItems();
        foreach (var (token, pair) in keysByToken)
        {
            pair.Rows.Should().EndWith($":sort:{token}:p1:s25");
            pair.Table.Should().Contain($":sort:{token}:");
        }
    }

    // "occurrences-desc" is an ALIAS of "occurrences": one ordering, one cache entry, byte-identical
    // to the key the pre-feature "occurrences" token already produced.
    [Theory]
    [InlineData("occurrences", "occurrences-desc")]
    [InlineData("alpha", "alpha-asc")]
    [InlineData("ayahs", "ayahs-desc")]
    [InlineData("surahs", "surahs-desc")]
    public void CacheKeys_AliasAndCanonicalSortToken_ShareOneEntry(string canonical, string alias)
    {
        var filter = new WordTypeFilter("noun", null, null, null, null);
        WordTypeSortParser.TryParse(canonical, out var canonicalSpec).Should().BeTrue();
        WordTypeSortParser.TryParse(alias, out var aliasSpec).Should().BeTrue();

        WordTypesCacheKeys.Rows(filter, aliasSpec, 1, 25)
            .Should().Be(WordTypesCacheKeys.Rows(filter, canonicalSpec, 1, 25));
        WordTypesCacheKeys.Table(filter, WordTypeTableView.Words, aliasSpec, 1, 25)
            .Should().Be(WordTypesCacheKeys.Table(filter, WordTypeTableView.Words, canonicalSpec, 1, 25));
    }

    // Scope counts and the grouped-detail reads describe a SCOPE, not an ordering: their keys take no
    // sort and must never absorb one, or a wider sort vocabulary would start fragmenting them.
    [Fact]
    public void CacheKeys_ScopeCountsAndGroupedDetails_StaySortFree()
    {
        var filter = new WordTypeFilter("noun", null, null, null, null);
        var selection = new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190700, filter);
        string[] keys =
        [
            WordTypesCacheKeys.ScopeCounts(filter),
            WordTypesCacheKeys.GroupedSummary(selection),
            WordTypesCacheKeys.GroupedWords(selection, 1, 25),
            WordTypesCacheKeys.GroupedAyahs(selection, 1, 25),
            WordTypesCacheKeys.GroupedSurahs(selection),
        ];

        // The ":sort:" segment is precisely what Rows/Table append and what a regression would add
        // here. A bare token scan would false-positive: the grouped-ayahs key legitimately carries
        // "ayahs" as its VIEW segment, which merely shares a name with the sort column.
        keys.Should().OnlyContain(key => !key.Contains(":sort:", StringComparison.Ordinal));
    }

    // Grouped by canonical token: mushaf-order collapses both directions onto one token.
    private static ILookup<string, WordTypeSortSpec> CanonicalSortSpecs() =>
        Enum.GetValues<WordTypeSortColumn>()
            .SelectMany(column => Enum.GetValues<WordSortDirection>()
                .Select(direction => new WordTypeSortSpec(column, direction)))
            .ToLookup(spec => spec.CanonicalToken());

    [Fact]
    public async Task CachedReader_TableRows_RootsAndStems_NeverCrossServe()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();
        var filter = new WordTypeFilter("noun", null, null, null, null);

        var rootsFirst = await reader.GetTableRowsAsync(filter, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);
        var rootsSecond = await reader.GetTableRowsAsync(filter, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);
        var stems = await reader.GetTableRowsAsync(filter, WordTypeTableView.Stems, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        // Same (filter, tableView, sort, page, pageSize) must hit the cache (identical reference);
        // a different tableView for the same filter must be a cache miss serving distinct content,
        // proving roots/stems never cross-serve through the shared IMemoryCache instance.
        rootsSecond.Should().BeSameAs(rootsFirst);
        stems.Should().NotBeSameAs(rootsFirst);
        rootsFirst.Items.Should().OnlyContain(row => row is RootTableRowDto);
        stems.Items.Should().OnlyContain(row => row is StemTableRowDto);
    }

    // Sorting is ORDER-ONLY: it is applied after filtering and touches ORDER BY alone, so no token may
    // change which rows the scope contains. Compared against the default token's own result rather than
    // a hardcoded expectation, so the assertion cannot drift with the seed.
    [Theory]
    [InlineData("occurrences-asc")]
    [InlineData("ayahs")]
    [InlineData("ayahs-asc")]
    [InlineData("surahs")]
    [InlineData("surahs-asc")]
    [InlineData("alpha")]
    [InlineData("alpha-desc")]
    [InlineData("mushaf-order")]
    public async Task WordsView_EverySortToken_PreservesTotalCountAndRowSet(string token)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();
        var filter = new WordTypeFilter("noun", null, null, null, null);

        WordTypeSortParser.TryParse(token, out var sort).Should().BeTrue();

        var baseline = await reader.GetTableRowsAsync(
            filter, WordTypeTableView.Words, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);
        var sorted = await reader.GetTableRowsAsync(
            filter, WordTypeTableView.Words, sort, 1, 25, CancellationToken.None);

        static IEnumerable<(int, string)> Identities(IEnumerable<WordTypeTableRowDto> rows) =>
            rows.Cast<WordTableRowDto>().Select(row => (row.TashkeelWordId, row.ContextCode));

        baseline.Items.Should().NotBeEmpty("the invariance claim is vacuous on an empty scope");
        sorted.TotalCount.Should().Be(baseline.TotalCount);
        Identities(sorted.Items).Should().BeEquivalentTo(Identities(baseline.Items));
    }
}
