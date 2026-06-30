using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesSubtypeReadTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task Tree_ReturnsCatalogueDrivenNounChildren_OrderedBySortOrder()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var noun = tree.MainTypes.Single(node => node.Code == "noun");
        // Seed POS catalogue has three noun-category codes: N, PN, ADJ (sort_order 1, 2, 3).
        noun.Children.Select(child => child.Code).Should().Equal("N", "PN", "ADJ");
        noun.Children.Should().OnlyContain(child => child.Code == child.ChildCode);
        noun.Children.Should().OnlyContain(child => child.Label.Ar.Length > 0);
    }

    [Fact]
    public async Task Tree_ReturnsFixedVerbTenseChildren_AndNoParticleOrInlChildren()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);

        var verb = tree.MainTypes.Single(node => node.Code == "verb");
        verb.Children.Select(child => child.ChildCode).Should().Equal("past", "present", "imperative");
        verb.Children.Select(child => child.Label.Ar).Should().Equal("ماض", "مضارع", "أمر");

        tree.MainTypes.Single(node => node.Code == "particle").Children.Should().BeEmpty();
        tree.MainTypes.Single(node => node.Code == "inl").Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData("N", 2)]
    [InlineData("PN", 1)]
    [InlineData("ADJ", 1)]
    public async Task NounChild_Count_Equals_ChildNodeCount_And_EqualsTableTotal(string childCode, int expectedCount)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);
        var rows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", childCode, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        var childNode = tree.MainTypes.Single(node => node.Code == "noun").Children
            .Single(child => child.ChildCode == childCode);

        childNode.Count.Should().Be(expectedCount);
        rows.TotalCount.Should().Be(expectedCount);
        rows.Items.Should().HaveCount(expectedCount);
        // A child node pins the head POS, so every row resolves to that exact subtype.
        rows.Items.Should().OnlyContain(row => row.TypeCode == childCode);
        rows.Items.Should().OnlyContain(row => row.ContextCode == childCode);
    }

    [Theory]
    [InlineData("past", 1)]
    [InlineData("present", 1)]
    [InlineData("imperative", 1)]
    public async Task VerbChild_Count_Equals_ChildNodeCount_And_EqualsTableTotal(string childCode, int expectedCount)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);
        var rows = await reader.GetRowsAsync(
            new WordTypeFilter("verb", childCode, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        var childNode = tree.MainTypes.Single(node => node.Code == "verb").Children
            .Single(child => child.ChildCode == childCode);

        childNode.Count.Should().Be(expectedCount);
        rows.TotalCount.Should().Be(expectedCount);
        rows.Items.Should().HaveCount(expectedCount);
        rows.Items.Should().OnlyContain(row => row.ContextCode == childCode);
    }

    [Fact]
    public async Task ChildRows_AreAStrictSubset_OfParentRows()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var parentRows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var childRows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", "PN", null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        childRows.TotalCount.Should().BeLessThanOrEqualTo(parentRows.TotalCount);
        childRows.Items.Should().OnlyContain(child =>
            parentRows.Items.Any(parent => parent.TashkeelWordId == child.TashkeelWordId));
    }

    [Fact]
    public async Task ChildRows_UseExactTypeLabels_FromPosCatalogue()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var rows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", "PN", null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        rows.Items.Should().OnlyContain(row => row.TypeLabel.Ar == "اسم علم");
        rows.Items.Should().OnlyContain(row => row.BroadLabel.Ar == "اسم");
    }

    [Theory]
    [InlineData("noun", "ZZZ", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("noun", "INL", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("verb", "future", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("verb", "N", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("particle", "P", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("inl", "INL", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    public async Task Handler_RejectsInvalidChildCode_ForType(
        string type,
        string childCode,
        Type expectedOutcome)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery(type, childCode, null, null, null, "occurrences", 1, 25),
            CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcome);
    }

    [Fact]
    public async Task TreeChildNode_Counts_HonorNoSecondaryFilterScoping()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // The E1 tree is intentionally unscoped by case/tense/voice; the noun N child carries the
        // full N word-context row count regardless of any active secondary filter on the rows read.
        var tree = await reader.GetTreeAsync(CancellationToken.None);
        var nounN = tree.MainTypes.Single(node => node.Code == "noun").Children
            .Single(child => child.ChildCode == "N");

        var unfilteredRows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", "N", null, null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        nounN.Count.Should().Be(unfilteredRows.TotalCount);
    }
}
