using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Caching.Quran.Words.WordTypes;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.WordTypes;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Feature 026 US6: tri-state has-root/has-stem/has-lemma presence flags on the shared Word Types list
// scope. The predicate lives on BaseRowsSql, so the words view, all three grouped views, and their
// TotalCounts reshape together; the seed's rootless/lemma-less/stem-less مُثَل identity vs the
// root/stem/lemma-bearing كَلِمَة identity make the tri-state observable.
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesPresenceFilterReadTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task HasRoot_ReshapesWordsView_TriState()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var all = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var present = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, HasRoot: true),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var missing = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, HasRoot: false),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        present.Items.Should().OnlyContain(row => row.RootText != null);
        present.Items.Should().NotContain(row => row.DisplayText == "مُثَل");

        missing.Items.Should().OnlyContain(row => row.RootText == null);
        missing.Items.Should().OnlyContain(row => row.DisplayText == "مُثَل");

        // Cleanly partitioned scope: every identity is either root-bearing or rootless.
        (present.TotalCount + missing.TotalCount).Should().Be(all.TotalCount);
        present.TotalCount.Should().BeGreaterThan(0);
        missing.TotalCount.Should().BeGreaterThan(0);
    }

    // Requiring the dimension the rootless identity LACKS keeps its words row but collapses that grouped
    // view to zero — proving the words view and the grouped views reshape from the same narrowed base.
    [Theory]
    [InlineData(WordTypeTableView.Roots)]
    [InlineData(WordTypeTableView.Stems)]
    [InlineData(WordTypeTableView.Lemmas)]
    public async Task PresenceMissing_KeepsWordsRow_ButCollapsesGroupedView(WordTypeTableView view)
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var filter = view switch
        {
            WordTypeTableView.Roots => new WordTypeFilter("noun", null, null, null, null, HasRoot: false),
            WordTypeTableView.Stems => new WordTypeFilter("noun", null, null, null, null, HasStem: false),
            WordTypeTableView.Lemmas => new WordTypeFilter("noun", null, null, null, null, HasLemma: false),
            _ => throw new ArgumentOutOfRangeException(nameof(view)),
        };

        var grouped = await reader.GetTableRowsAsync(filter, view, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var words = await reader.GetTableRowsAsync(filter, WordTypeTableView.Words, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        grouped.TotalCount.Should().Be(0);
        grouped.Items.Should().BeEmpty();
        words.TotalCount.Should().BeGreaterThan(0);
    }

    // Requiring a present dimension is a no-op relative to the unfiltered grouped view (both exclude nulls),
    // confirming the grouped total is measured over the same scoped base after the presence predicate.
    [Fact]
    public async Task HasRootTrue_MatchesUnfilteredRootsGroupedTotal()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var unfiltered = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null),
            WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var hasRoot = await reader.GetTableRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, HasRoot: true),
            WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        hasRoot.TotalCount.Should().Be(unfiltered.TotalCount);
        hasRoot.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PresenceFlags_ComposeWithSearch()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        // مُثَل matches search "مثل" and is rootless: requiring a root drops it, requiring none keeps it.
        var mithalHasRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل", HasRoot: true),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var mithalNoRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "مثل", HasRoot: false),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        // كَلِمَة matches search "كلم" and is root-bearing: requiring no root drops it.
        var kalimaNoRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "كلم", HasRoot: false),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var kalimaHasRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, null, null, null, "كلم", HasRoot: true),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        mithalHasRoot.TotalCount.Should().Be(0);
        mithalNoRoot.TotalCount.Should().BeGreaterThan(0);
        kalimaNoRoot.TotalCount.Should().Be(0);
        kalimaHasRoot.TotalCount.Should().BeGreaterThan(0);
    }

    // Feature 026 D2: presence flags AND with the noun case sub-filter. Under type=noun, case=nominative
    // the seed has two identities — root-bearing كَلِمَة and rootless مُثَل — so a presence flag must
    // partition WITHIN the case scope, not ignore it.
    [Fact]
    public async Task PresenceFlags_ComposeWithNounCaseFilter()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var all = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "nominative", null, null),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var hasRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "nominative", null, null, HasRoot: true),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var noRoot = await reader.GetRowsAsync(
            new WordTypeFilter("noun", null, "nominative", null, null, HasRoot: false),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        all.TotalCount.Should().Be(2);
        hasRoot.Items.Should().OnlyContain(row => row.RootText != null && row.CaseOrFeature == "nominative");
        hasRoot.Items.Should().NotContain(row => row.DisplayText == "مُثَل");
        noRoot.Items.Should().OnlyContain(row => row.RootText == null && row.CaseOrFeature == "nominative");
        noRoot.Items.Should().OnlyContain(row => row.DisplayText == "مُثَل");
        (hasRoot.TotalCount + noRoot.TotalCount).Should().Be(all.TotalCount);
        hasRoot.TotalCount.Should().BeGreaterThan(0);
        noRoot.TotalCount.Should().BeGreaterThan(0);
    }

    // Feature 026 D2: presence flags AND with the verb tense/voice sub-filters. The flag partitions the
    // tense-scoped rows by root presence while every row stays within the tense scope (ContextCode).
    [Fact]
    public async Task PresenceFlags_ComposeWithVerbTenseFilter()
    {
        await using var scope = fixture.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<EfWordTypesReader>();

        var all = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", null),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var hasRoot = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", null, HasRoot: true),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);
        var noRoot = await reader.GetRowsAsync(
            new WordTypeFilter("verb", null, null, "present", null, HasRoot: false),
            WordTypeSortSpec.Default, 1, 1000, CancellationToken.None);

        all.TotalCount.Should().BeGreaterThan(0);
        hasRoot.Items.Should().OnlyContain(row => row.RootText != null && row.ContextCode == "present");
        noRoot.Items.Should().OnlyContain(row => row.RootText == null && row.ContextCode == "present");
        (hasRoot.TotalCount + noRoot.TotalCount).Should().Be(all.TotalCount);
    }

    // Presence flags fold into the rows/table cache key: absent flags keep the pre-feature key (warm
    // entries stay valid); every flag value isolates its entry so flagged and unflagged reads never
    // cross-serve.
    [Fact]
    public void PresenceFlags_FoldIntoCacheKey_AndKeepAbsentStable()
    {
        var noFlags = new WordTypeFilter("noun", null, null, null, null);
        var explicitlyNoFlags = new WordTypeFilter("noun", null, null, null, null, null, null, null, null);

        var noFlagsKey = WordTypesCacheKeys.Rows(noFlags, WordTypeSortSpec.Default, 1, 1000);
        WordTypesCacheKeys.Rows(explicitlyNoFlags, WordTypeSortSpec.Default, 1, 1000).Should().Be(noFlagsKey);

        var keys = new[]
        {
            noFlagsKey,
            WordTypesCacheKeys.Rows(new WordTypeFilter("noun", null, null, null, null, HasRoot: true), WordTypeSortSpec.Default, 1, 1000),
            WordTypesCacheKeys.Rows(new WordTypeFilter("noun", null, null, null, null, HasRoot: false), WordTypeSortSpec.Default, 1, 1000),
            WordTypesCacheKeys.Rows(new WordTypeFilter("noun", null, null, null, null, HasStem: true), WordTypeSortSpec.Default, 1, 1000),
            WordTypesCacheKeys.Rows(new WordTypeFilter("noun", null, null, null, null, HasLemma: false), WordTypeSortSpec.Default, 1, 1000),
        };
        keys.Should().OnlyHaveUniqueItems();

        var noFlagsTable = WordTypesCacheKeys.Table(noFlags, WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000);
        WordTypesCacheKeys.Table(new WordTypeFilter("noun", null, null, null, null, HasRoot: true), WordTypeTableView.Roots, WordTypeSortSpec.Default, 1, 1000)
            .Should().NotBe(noFlagsTable);
    }
}
