using Microsoft.EntityFrameworkCore;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesCacheReadTests(WordTypesTestFixture fixture)
{
    [Fact]
    public void CacheKeys_DoNotExposeRawTextOrIdentityValues()
    {
        var filter = new WordTypeFilter("noun", "PN", "genitive", null, null);
        var identity = new WordTypeRowIdentity(191001, "PN", "genitive", null, null);

        var rowsKey = WordTypesCacheKeys.Rows(filter, WordTypeSortSpec.Default, 2, 25);
        var summaryKey = WordTypesCacheKeys.Summary(identity);
        var ayahsKey = WordTypesCacheKeys.Ayahs(identity, 3, 10);
        var surahsKey = WordTypesCacheKeys.Surahs(identity);

        rowsKey.Should().StartWith("wordtypes:rows:");
        rowsKey.Should().Contain(":sort:occurrences:p2:s25");
        rowsKey.Should().NotContain("noun");
        rowsKey.Should().NotContain("PN");
        rowsKey.Should().NotContain("genitive");

        summaryKey.Should().StartWith("wordtypes:summary:");
        summaryKey.Should().NotContain("191001");
        summaryKey.Should().NotContain("PN");
        summaryKey.Should().NotContain("genitive");

        ayahsKey.Should().StartWith("wordtypes:ayahs:");
        ayahsKey.Should().Contain(":p3:s10");
        ayahsKey.Should().NotContain("191001");
        ayahsKey.Should().NotContain("PN");
        ayahsKey.Should().NotContain("genitive");

        surahsKey.Should().StartWith("wordtypes:surahs:");
        surahsKey.Should().NotContain("191001");
        surahsKey.Should().NotContain("PN");
        surahsKey.Should().NotContain("genitive");
    }

    // Feature 026 US1: the normalized search folds into the rows/table cache key. An absent search keeps
    // the pre-feature key (warm entries stay valid); two raw terms that normalize equally share one entry;
    // a searched read never cross-serves an unsearched one.
    [Fact]
    public void RowsAndTableCacheKeys_FoldInNormalizedSearch_AndKeepEmptySearchStable()
    {
        var noSearch = new WordTypeFilter("noun", null, null, null, null);
        var blankSearch = new WordTypeFilter("noun", null, null, null, null, "   ");
        var searched = new WordTypeFilter("noun", null, null, null, null, "كلم");
        var searchedEquivalent = new WordTypeFilter("noun", null, null, null, null, "كَلِم");
        var searchedOther = new WordTypeFilter("noun", null, null, null, null, "مثل");

        var noSearchRows = WordTypesCacheKeys.Rows(noSearch, WordTypeSortSpec.Default, 1, 1000);

        // Empty/whitespace search normalizes away → identical key to no search at all.
        WordTypesCacheKeys.Rows(blankSearch, WordTypeSortSpec.Default, 1, 1000).Should().Be(noSearchRows);

        // A real search isolates the entry; two terms that normalize to the same skeleton collide.
        var searchedRows = WordTypesCacheKeys.Rows(searched, WordTypeSortSpec.Default, 1, 1000);
        searchedRows.Should().NotBe(noSearchRows);
        WordTypesCacheKeys.Rows(searchedEquivalent, WordTypeSortSpec.Default, 1, 1000).Should().Be(searchedRows);
        WordTypesCacheKeys.Rows(searchedOther, WordTypeSortSpec.Default, 1, 1000).Should().NotBe(searchedRows);

        // The raw search text never leaks into the key.
        searchedRows.Should().NotContain("كلم");

        // Same isolation on the table key.
        var noSearchTable = WordTypesCacheKeys.Table(noSearch, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000);
        WordTypesCacheKeys.Table(searched, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000)
            .Should().NotBe(noSearchTable);
    }

    [Fact]
    public async Task CachedReader_RepeatedReads_DoNotIssueExtraSqlCommands()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedWordTypesReader(new EfWordTypesReader(dbContext), cache);

        interceptor.Reset();
        var firstTree = await reader.GetTreeAsync(CancellationToken.None);
        var treeCommandCount = interceptor.CommandCount;
        var secondTree = await reader.GetTreeAsync(CancellationToken.None);

        secondTree.Should().BeSameAs(firstTree);
        interceptor.CommandCount.Should().Be(treeCommandCount);
        treeCommandCount.Should().BeGreaterThan(0);

        var filter = new WordTypeFilter("noun", null, null, null, null);

        interceptor.Reset();
        var firstRows = await reader.GetRowsAsync(filter, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);
        var rowsCommandCount = interceptor.CommandCount;
        var secondRows = await reader.GetRowsAsync(filter, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        secondRows.Should().BeSameAs(firstRows);
        interceptor.CommandCount.Should().Be(rowsCommandCount);
        rowsCommandCount.Should().BeGreaterThan(0);
    }

    // Grouped detail keys isolate kind, dimension ID, and the full five-field scope, and never share a
    // view prefix. Paged views (words/ayahs) carry page/pageSize; single-shot views (summary/surahs) do not.
    [Fact]
    public void GroupedDetailsCacheKeys_IsolateKindIdScopeViewAndApplicablePage()
    {
        var selection = new WordTypeGroupedSelection(
            WordTypeGroupedDimensionKind.Root, 190700, new WordTypeFilter("noun", null, null, null, null));

        var summary = WordTypesCacheKeys.GroupedSummary(selection);
        var words = WordTypesCacheKeys.GroupedWords(selection, 1, 25);
        var ayahs = WordTypesCacheKeys.GroupedAyahs(selection, 1, 25);
        var surahs = WordTypesCacheKeys.GroupedSurahs(selection);

        new[] { summary, words, ayahs, surahs }.Should().OnlyHaveUniqueItems();
        summary.Should().StartWith("wordtypes:grouped:roots:summary:");
        words.Should().StartWith("wordtypes:grouped:roots:words:");
        ayahs.Should().StartWith("wordtypes:grouped:roots:ayahs:");
        surahs.Should().StartWith("wordtypes:grouped:roots:surahs:");

        words.Should().Contain(":p1:s25");
        ayahs.Should().Contain(":p1:s25");
        summary.Should().NotContain(":p");
        surahs.Should().NotContain(":p");

        WordTypesCacheKeys.GroupedSurahs(selection with { Kind = WordTypeGroupedDimensionKind.Stem })
            .Should().NotBe(surahs);
        WordTypesCacheKeys.GroupedSurahs(selection with { DimensionId = 190600 })
            .Should().NotBe(surahs);
        WordTypesCacheKeys.GroupedSurahs(selection with { Filter = new WordTypeFilter("noun", null, "nominative", null, null) })
            .Should().NotBe(surahs);

        WordTypesCacheKeys.GroupedWords(selection, 2, 25).Should().NotBe(words);
        WordTypesCacheKeys.GroupedAyahs(selection, 1, 10).Should().NotBe(ayahs);
    }

    [Fact]
    public void GroupedSummaryAndSurahsCacheKeys_AreUniqueForEverySelectionField()
    {
        var selection = new WordTypeGroupedSelection(
            WordTypeGroupedDimensionKind.Root,
            190700,
            new WordTypeFilter("noun", null, null, null, null));
        WordTypeGroupedSelection[] selections =
        [
            selection,
            selection with { Kind = WordTypeGroupedDimensionKind.Stem },
            selection with { Kind = WordTypeGroupedDimensionKind.Lemma },
            selection with { DimensionId = 190600 },
            selection with { Filter = selection.Filter with { Type = "verb" } },
            selection with { Filter = selection.Filter with { ChildCode = "PN" } },
            selection with { Filter = selection.Filter with { Case = "genitive" } },
            selection with { Filter = selection.Filter with { Tense = "past" } },
            selection with { Filter = selection.Filter with { Voice = "active" } },
        ];

        var summaryKeys = selections.Select(WordTypesCacheKeys.GroupedSummary).ToArray();
        var surahsKeys = selections.Select(WordTypesCacheKeys.GroupedSurahs).ToArray();

        summaryKeys.Should().OnlyHaveUniqueItems();
        surahsKeys.Should().OnlyHaveUniqueItems();
        summaryKeys.Concat(surahsKeys).Should().OnlyHaveUniqueItems();
    }

    // A second read of every grouped view is served from the cache without issuing new SQL commands.
    [Fact]
    public async Task GroupedDetailsCachedReader_RepeatedReadsDoNotIssueExtraCommands()
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedWordTypesReader(new EfWordTypesReader(dbContext), cache);

        var selection = new WordTypeGroupedSelection(
            WordTypeGroupedDimensionKind.Root, 190700, new WordTypeFilter("noun", null, null, null, null));

        await AssertSecondReadHitsCache(interceptor, () => reader.GetGroupedSummaryAsync(selection, CancellationToken.None));
        await AssertSecondReadHitsCache(interceptor, () => reader.GetGroupedMemberWordsAsync(selection, 1, 25, CancellationToken.None));
        await AssertSecondReadHitsCache(interceptor, () => reader.GetGroupedAyahMatchesAsync(selection, 1, 25, CancellationToken.None));
        await AssertSecondReadHitsCache(interceptor, () => reader.GetGroupedSurahsAsync(selection, CancellationToken.None));
    }

    private static async Task AssertSecondReadHitsCache<T>(SqlCommandCountInterceptor interceptor, Func<Task<T>> read)
    {
        interceptor.Reset();
        _ = await read();
        var firstCount = interceptor.CommandCount;
        _ = await read();

        interceptor.CommandCount.Should().Be(firstCount);
        firstCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RepeatedTreeAndRowsReads_ReturnCachedInstances()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var firstTree = await reader.GetTreeAsync(CancellationToken.None);
        var secondTree = await reader.GetTreeAsync(CancellationToken.None);

        var filter = new WordTypeFilter("noun", null, null, null, null);
        var firstRows = await reader.GetRowsAsync(filter, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);
        var secondRows = await reader.GetRowsAsync(filter, WordTypeSortSpec.Default, 1, 25, CancellationToken.None);

        secondTree.Should().BeSameAs(firstTree);
        secondRows.Should().BeSameAs(firstRows);
    }
}
