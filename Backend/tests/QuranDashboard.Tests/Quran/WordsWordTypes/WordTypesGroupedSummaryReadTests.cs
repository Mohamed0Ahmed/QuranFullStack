using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesGroupedSummaryReadTests(WordTypesTestFixture fixture)
{
    private static readonly WordTypeFilter NounScope = new("noun", null, null, null, null);

    // Selecting a grouped root/stem/lemma row and opening its scoped summary must yield the exact
    // same identity and three measures the grouped table row already displayed in the same scope.
    [Theory]
    [InlineData(WordTypeGroupedDimensionKind.Root, WordTypeTableView.Roots, 190700)]
    [InlineData(WordTypeGroupedDimensionKind.Stem, WordTypeTableView.Stems, 190600)]
    [InlineData(WordTypeGroupedDimensionKind.Lemma, WordTypeTableView.Lemmas, 190500)]
    public async Task GroupedSummary_RootStemLemma_InSameScope_MatchesSelectedTableRow(
        WordTypeGroupedDimensionKind kind,
        WordTypeTableView view,
        int dimensionId)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var tablePage = await reader.GetTableRowsAsync(
            NounScope, view, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var row = ExtractGroupedRow(tablePage.Items.Single());

        var summary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(kind, dimensionId, NounScope),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.Kind.Should().Be(kind.ToDtoKind());
        summary.DimensionId.Should().Be(dimensionId).And.Be(row.Id);
        summary.DisplayText.Should().Be(row.DisplayText);
        summary.OccurrencesCount.Should().Be(row.OccurrencesCount);
        summary.AyahsCount.Should().Be(row.AyahsCount);
        summary.SurahsCount.Should().Be(row.SurahsCount);
    }

    // An active secondary filter must re-scope the base before aggregating, so the summary reports the
    // filtered counts, never the dimension's global totals.
    [Fact]
    public async Task GroupedSummary_ActiveSecondaryFilter_RecomputesTheSameScopedCounts()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        // Verb root 190701 (ع ل م) spans 3 occurrences overall (past/present/imperative); tense=past
        // narrows the base to a single occurrence (1907001) in one ayah of one surah.
        var summary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(
                WordTypeGroupedDimensionKind.Root,
                190701,
                new WordTypeFilter("verb", null, null, "past", null)),
            CancellationToken.None);

        summary.Should().NotBeNull();
        summary!.OccurrencesCount.Should().Be(1);
        summary.AyahsCount.Should().Be(1);
        summary.SurahsCount.Should().Be(1);
    }

    // A dimension that only exists in a different scope must not resolve; the verb-only root 190701 has
    // no rows once the base is narrowed to the noun scope.
    [Fact]
    public async Task GroupedSummary_DimensionOutsideScope_ReturnsNull()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var summary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190701, NounScope),
            CancellationToken.None);

        summary.Should().BeNull();
    }

    // Grouped membership comes from head-level quran_word_morphology only. A fixture segment on word
    // 1903001 carries the alternate dimension IDs 190701/190502/190602, but those must never surface in
    // the noun scope and must never displace the word's real head IDs 190700/190500/190600.
    [Fact]
    public async Task GroupedSummary_HeadDimensionIgnoresSecondarySegmentDimension()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var segmentRoot = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190701, NounScope),
            CancellationToken.None);
        var segmentLemma = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Lemma, 190502, NounScope),
            CancellationToken.None);
        var segmentStem = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Stem, 190602, NounScope),
            CancellationToken.None);

        segmentRoot.Should().BeNull();
        segmentLemma.Should().BeNull();
        segmentStem.Should().BeNull();

        var headRoot = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Root, 190700, NounScope),
            CancellationToken.None);
        var headLemma = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Lemma, 190500, NounScope),
            CancellationToken.None);
        var headStem = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Stem, 190600, NounScope),
            CancellationToken.None);

        headRoot!.OccurrencesCount.Should().Be(3);
        headLemma!.OccurrencesCount.Should().Be(3);
        headStem!.OccurrencesCount.Should().Be(3);
    }

    // Markers and null head dimensions must never contribute a grouped dimension. The noun WORDS view
    // counts 4 word-context occurrences; the head-grain lemma grouping counts only the 3 real
    // lemma-bearing rows, so the difference is exactly the excluded null-dimension occurrence (مُثَل)
    // and the marker word 1903004 leaks nothing in.
    [Fact]
    public async Task GroupedSummary_MarkerAndNullDimensionsRemainExcluded()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWordTypesReader>();

        var lemmaSummary = await reader.GetGroupedSummaryAsync(
            new WordTypeGroupedSelection(WordTypeGroupedDimensionKind.Lemma, 190500, NounScope),
            CancellationToken.None);
        var wordsPage = await reader.GetTableRowsAsync(
            NounScope, WordTypeTableView.Words, WordTypeSort.Occurrences, 1, 25, CancellationToken.None);
        var wordsOccurrenceSum = wordsPage.Items.Cast<WordTableRowDto>().Sum(item => item.OccurrencesCount);

        lemmaSummary.Should().NotBeNull();
        lemmaSummary!.OccurrencesCount.Should().Be(3);
        wordsOccurrenceSum.Should().Be(4);
        (wordsOccurrenceSum - lemmaSummary.OccurrencesCount).Should().Be(1).And.NotBe(0);
    }

    // The handler maps each failure mode to its own controlled outcome, validating in order:
    // route kind, positive dimension ID, grammatical filter, then reader result.
    [Fact]
    public async Task GroupedSummaryHandler_InvalidKindIdFilterAndMissingGroup_MapToControlledOutcomes()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeGroupedSummaryHandler>();

        var invalidKind = await handler.HandleAsync(
            new GetWordTypeGroupedSummaryQuery("bogus", 190700, "noun", null, null, null, null),
            CancellationToken.None);
        var invalidId = await handler.HandleAsync(
            new GetWordTypeGroupedSummaryQuery("roots", 0, "noun", null, null, null, null),
            CancellationToken.None);
        var invalidFilter = await handler.HandleAsync(
            new GetWordTypeGroupedSummaryQuery("roots", 190700, "noun", null, null, "past", null),
            CancellationToken.None);
        var notFound = await handler.HandleAsync(
            new GetWordTypeGroupedSummaryQuery("roots", 190701, "noun", null, null, null, null),
            CancellationToken.None);

        invalidKind.Should().BeOfType<GetWordTypeGroupedSummaryOutcome.InvalidKind>();
        invalidId.Should().BeOfType<GetWordTypeGroupedSummaryOutcome.InvalidId>();
        invalidFilter.Should().BeOfType<GetWordTypeGroupedSummaryOutcome.InvalidFilter>();
        notFound.Should().BeOfType<GetWordTypeGroupedSummaryOutcome.NotFound>();
    }

    private static GroupedRowFacts ExtractGroupedRow(WordTypeTableRowDto row) => row switch
    {
        RootTableRowDto root => new GroupedRowFacts(
            root.RootId, root.DisplayText, root.OccurrencesCount, root.AyahsCount, root.SurahsCount),
        StemTableRowDto stem => new GroupedRowFacts(
            stem.StemId, stem.DisplayText, stem.OccurrencesCount, stem.AyahsCount, stem.SurahsCount),
        LemmaTableRowDto lemma => new GroupedRowFacts(
            lemma.LemmaId, lemma.DisplayText, lemma.OccurrencesCount, lemma.AyahsCount, lemma.SurahsCount),
        _ => throw new InvalidOperationException($"Unexpected grouped row type {row.GetType().Name}."),
    };

    private sealed record GroupedRowFacts(
        int Id, string DisplayText, int OccurrencesCount, int AyahsCount, int SurahsCount);
}
