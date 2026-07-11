using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesMainReadTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task Tree_ReturnsFourMainTypes_WithCorrectMainCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        tree.MainTypes.Select(node => node.Code).Should().Equal("noun", "verb", "particle", "inl");
        Count(tree, "noun").Should().Be(3);
        Count(tree, "verb").Should().Be(3);
        Count(tree, "particle").Should().Be(1);
        Count(tree, "inl").Should().Be(1);
    }

    [Theory]
    [InlineData("noun", 4)]
    [InlineData("verb", 4)]
    [InlineData("particle", 1)]
    [InlineData("inl", 2)]
    public async Task Rows_ReturnExpectedTotals_ForMainTypes(string type, int expectedCount)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var rows = await reader.GetRowsAsync(
            new WordTypeFilter(type, null, null, null, null),
            WordTypeSort.Occurrences,
            page: 1,
            pageSize: 25,
            CancellationToken.None);

        rows.TotalCount.Should().Be(expectedCount);
        rows.Items.Should().HaveCount(expectedCount);
        rows.Items.Should().OnlyContain(row => row.DisplayText.Length > 0);
        rows.Items.Should().OnlyContain(row => row.TashkeelWordId > 0);
        rows.Items.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.ContextCode));
    }

    [Fact]
    public async Task ParticleRows_ExcludeInl_AndInlRowsStayIsolated()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var particleRows = await reader.GetRowsAsync(
            new WordTypeFilter("particle", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var inlRows = await reader.GetRowsAsync(
            new WordTypeFilter("inl", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        particleRows.Items.Should().ContainSingle(row => row.ContextCode == "PRO");
        particleRows.Items.Should().NotContain(row => row.ContextCode == "INL");
        // inl has two word-context rows (191002 الٓمٓ, 191009 ص) — both isolated from particle.
        inlRows.Items.Should().HaveCount(2);
        inlRows.Items.Should().OnlyContain(row => row.ContextCode == "INL");
    }

    [Fact]
    public async Task Rows_ExcludeMarkersAndOutOfBucketPos_AndRetainNullEnrichmentRows()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var nounRows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var particleRows = await reader.GetRowsAsync(
            new WordTypeFilter("particle", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        nounRows.Items.Should().NotContain(row => row.DisplayText == "ۚ");
        nounRows.Items.Should().NotContain(row => row.DisplayText == "خَارِج");
        particleRows.Items.Should().ContainSingle(row => row.DisplayText == "لَا" && row.RootText == null);
    }

    [Fact]
    public async Task Rows_ReturnScopedCounts_AndDeterministicFirstOccurrenceOrder()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var rows = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        rows.Items.Select(row => row.ContextCode).Should().Equal("unspecified", "past", "present", "imperative");
        rows.Items.Should().OnlyContain(row => row.OccurrencesCount == 1 && row.AyahsCount == 1 && row.SurahsCount == 1);
    }

    [Fact]
    public async Task Rows_MushafOrder_UsesFirstOccurrenceOrder_NotTashkeelIdentity()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var rows = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, null, null),
            WordTypeSort.MushafOrder,
            1,
            25,
            CancellationToken.None);

        rows.Items.Select(row => row.ContextCode).Should().Equal("unspecified", "past", "present", "imperative");
    }

    [Theory]
    [InlineData("bad", "occurrences", 1, 25, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("noun", "bad", 1, 25, typeof(GetWordTypeRowsOutcome.InvalidSort))]
    [InlineData("noun", "occurrences", 0, 25, typeof(GetWordTypeRowsOutcome.InvalidPaging))]
    public async Task Handler_ReturnsControlledValidationOutcomes(
        string type,
        string sort,
        int page,
        int pageSize,
        Type expectedOutcome)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery(type, null, null, null, null, sort, page, pageSize),
            CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcome);
    }

    private static int Count(WordTypeTreeDto tree, string code) =>
        tree.MainTypes.Single(node => node.Code == code).Count;
}
