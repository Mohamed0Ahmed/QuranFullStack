using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

        var rowsKey = WordTypesCacheKeys.Rows(filter, WordTypeSort.Occurrences, 2, 25);
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
        var firstRows = await reader.GetRowsAsync(filter, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var rowsCommandCount = interceptor.CommandCount;
        var secondRows = await reader.GetRowsAsync(filter, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);

        secondRows.Should().BeSameAs(firstRows);
        interceptor.CommandCount.Should().Be(rowsCommandCount);
        rowsCommandCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RepeatedTreeAndRowsReads_ReturnCachedInstances()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var firstTree = await reader.GetTreeAsync(CancellationToken.None);
        var secondTree = await reader.GetTreeAsync(CancellationToken.None);

        var filter = new WordTypeFilter("noun", null, null, null, null);
        var firstRows = await reader.GetRowsAsync(filter, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var secondRows = await reader.GetRowsAsync(filter, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);

        secondTree.Should().BeSameAs(firstTree);
        secondRows.Should().BeSameAs(firstRows);
    }
}
