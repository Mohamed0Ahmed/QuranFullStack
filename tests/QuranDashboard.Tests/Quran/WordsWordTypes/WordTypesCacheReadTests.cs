using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesCacheReadTests(WordTypesTestFixture fixture)
{
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
