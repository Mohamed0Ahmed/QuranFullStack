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
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var genitive = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "genitive", null, null),
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        nominative.Items.Should().OnlyContain(row => row.CaseOrFeature == "nominative");
        // N nominative occurrences: كَلِمَة (1903001) and مُثَل (1903011).
        nominative.TotalCount.Should().Be(2);
        // PN genitive occurrence: كَلِمَة (1903002).
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
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // ADJ occurrence with NULL case_feature: كَلِمَة (1903003).
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
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // The E1 tree is intentionally unscoped by secondary filters, so an active case filter must
        // narrow the table total below the static node count (FR-027 / data-model §4.3).
        filteredRows.TotalCount.Should().BeLessThan(nounNodeCount);
    }

    [Fact]
    public async Task VerbTenseFilter_NarrowsRows_ToThatTense()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var past = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "past", null),
            WordTypeSort.Occurrences,
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
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);

        // Only عُلِمَ (1907002) is passive.
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
            WordTypeSort.Occurrences,
            1,
            25,
            CancellationToken.None);
        var presentActive = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", "active"),
            WordTypeSort.Occurrences,
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

        // "all" is the frontend default; particle accepts it without a controlled failure.
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

        // The PN genitive row under كَلِمَة must reproduce via the summary identity carrying the case.
        var summary = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "PN", "genitive", null, null),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.ContextCode.Should().Be("PN");
        summary.CaseOrFeature.Should().Be("genitive");
        // Case pin narrows detail reads to the single genitive PN occurrence (1903002), not all three كَلِمَة rows.
        summary.OccurrencesCount.Should().Be(1);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);

        var wrongCase = await reader.GetSummaryAsync(
            new WordTypeRowIdentity(KalimaTashkeelId, "PN", "nominative", null, null),
            CancellationToken.None);
        wrongCase.Should().BeNull();
    }
}
