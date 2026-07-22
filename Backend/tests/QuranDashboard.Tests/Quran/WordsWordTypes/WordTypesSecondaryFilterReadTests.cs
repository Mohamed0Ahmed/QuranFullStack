using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesSecondaryFilterReadTests(WordTypesTestFixture fixture)
{
    private const int KalimaTashkeelId = 191001;

    [Fact]
    public async Task NounCaseFilter_NarrowsRows_ToThatCase()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var nominative = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "nominative", null, null),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);
        var genitive = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "genitive", null, null),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        nominative.Items.Should().OnlyContain(row => row.CaseOrFeature == "nominative");
        nominative.TotalCount.Should().Be(2);
        genitive.TotalCount.Should().Be(1);
        genitive.Items.Should().OnlyContain(row => row.ContextCode == "PN");
    }

    [Fact]
    public async Task NounCaseFilter_NullValue_MatchesNullCaseFeature()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var unspecified = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "null", null, null),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        unspecified.TotalCount.Should().Be(1);
        unspecified.Items.Should().OnlyContain(row => row.ContextCode == "ADJ");
        unspecified.Items.Should().OnlyContain(row => row.CaseOrFeature == null);
    }

    [Fact]
    public async Task NounCaseFilter_TotalCount_IsNotTreeCount_WhileFilterActive()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tree = await reader.GetTreeAsync(CancellationToken.None);
        var nounNodeCount = tree.MainTypes.Single(node => node.Code == "noun").Count;

        var filteredRows = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "nominative", null, null),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        filteredRows.TotalCount.Should().BeLessThan(nounNodeCount);
    }

    [Fact]
    public async Task VerbTenseFilter_NarrowsRows_ToThatTense()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var past = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "past", null),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        past.TotalCount.Should().Be(1);
        past.Items.Should().OnlyContain(row => row.ContextCode == "past");
    }

    [Fact]
    public async Task VerbVoiceFilter_NarrowsRows_ToThatVoice()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var passive = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, null, "passive"),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        passive.TotalCount.Should().Be(1);
        passive.Items.Should().OnlyContain(row => row.ContextCode == "present");
    }

    [Fact]
    public async Task VerbTenseAndVoiceFilter_CombineByConjunction()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var presentPassive = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", "passive"),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);
        var presentActive = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", "active"),
            WordTypeSortSpec.Default,
            1,
            25,
            CancellationToken.None);

        presentPassive.TotalCount.Should().Be(1);
        presentActive.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData("verb", "nominative", null, null, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("particle", "nominative", null, null, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("inl", "nominative", null, null, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("noun", null, "past", null, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("noun", null, null, "active", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("particle", null, "past", "active", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("noun", "bogus", null, null, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData("verb", null, null, "bogus", typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    public async Task Handler_RejectsSecondaryFilter_WhenInvalidForType(
        string type,
        string? @case,
        string? tense,
        string? voice,
        Type expectedOutcome)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery(type, null, @case, tense, voice, null, "occurrences", 1, 25),
            CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcome);
    }

    [Fact]
    public async Task Handler_AcceptsAllSentinel_AsNoFilter_ForAnyType()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery("particle", null, "all", "all", "all", null, "occurrences", 1, 25),
            CancellationToken.None);

        outcome.Should().BeOfType<GetWordTypeRowsOutcome.Success>();
    }

    [Fact]
    public async Task DetailIdentity_UnderCaseFilter_RecoversSameRowContext()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var summary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "PN", "genitive", null, null),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.ContextCode.Should().Be("PN");
        summary.CaseOrFeature.Should().Be("genitive");
        summary.OccurrencesCount.Should().Be(1);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);

        var wrongCase = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "PN", "nominative", null, null),
            CancellationToken.None);
        wrongCase.Should().BeNull();
    }
}
