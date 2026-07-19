using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

// Feature 026, US7 — association filters on the Unique Words list. The primaryType/rootId predicates
// live in the base SQL and reproduce the exact primary-selection rule the displayed chip enrichment
// uses, so for every filtered row the displayed primary value equals the filter (chip⇔filter).
// Agreement tests are data-driven from an unfiltered read; the discrimination tests below pin the
// seed's PRIMARY-vs-ANY scenarios, which fail under a naive "any occurrence matches" predicate
// (the exact FR-009 regression this suite exists to catch).
[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsAssociationFilterTests(UniqueWordsTestFixture fixture)
{
    private const int UnmatchedButValidRootId = 999_999;

    // Seed discrimination scenario (see unique-words-seed.sql): الله (1002) has primary type PN (×3)
    // with a MINORITY N occurrence, and primary root 5001 'أ ل ه' (×3) with one SYNTHETIC minority
    // co-occurrence of root 5003 'ر ح م'; آمنوا (2003) has primary type V with a MINORITY N
    // occurrence. N and ADJ are catalogued but never primary anywhere in the slice.
    private const int AllahUniqueWordId = 1002;
    private const int AmanuUniqueWordId = 2003;
    private const int DominantRootOfAllah = 5001;
    private const int MinorityCoOccurringRootId = 5003;
    private const string MinorityOnlyTypeCode = "N";
    private const string CataloguedNeverOccurringTypeCode = "ADJ";

    [Fact]
    public async Task PrimaryType_filter_keeps_only_rows_whose_displayed_primary_type_equals_the_filter()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var all = (await ReadAsync(handler, UniqueWordsAssociationFilter.None)).Items;
        var primaryType = all.First(i => i.PrimaryWordTypeCode is not null).PrimaryWordTypeCode!;
        var expected = all.Where(i => i.PrimaryWordTypeCode == primaryType).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(handler, UniqueWordsAssociationFilter.FromRaw(primaryType, null));

        filtered.Items.Should().OnlyContain(i => i.PrimaryWordTypeCode == primaryType,
            "the base-SQL winner predicate and the displayed-chip enrichment share one selection rule");
        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count, "the filtered TotalCount is the stat contract");
    }

    [Fact]
    public async Task PrimaryRoot_filter_keeps_only_rows_whose_displayed_primary_root_equals_the_filter()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var all = (await ReadAsync(handler, UniqueWordsAssociationFilter.None)).Items;
        var rootId = all.First(i => i.RootId is not null).RootId!.Value;
        var expected = all.Where(i => i.RootId == rootId).Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(handler, UniqueWordsAssociationFilter.FromRaw(null, rootId));

        filtered.Items.Should().OnlyContain(i => i.RootId == rootId);
        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task PrimaryType_and_root_compose_as_intersection_with_the_displayed_chips()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var all = (await ReadAsync(handler, UniqueWordsAssociationFilter.None)).Items;
        var seed = all.First(i => i.PrimaryWordTypeCode is not null && i.RootId is not null);
        var expected = all
            .Where(i => i.PrimaryWordTypeCode == seed.PrimaryWordTypeCode && i.RootId == seed.RootId)
            .Select(i => i.Id).OrderBy(id => id).ToList();

        var filtered = await ReadAsync(
            handler, UniqueWordsAssociationFilter.FromRaw(seed.PrimaryWordTypeCode, seed.RootId));

        filtered.Items.Should().OnlyContain(
            i => i.PrimaryWordTypeCode == seed.PrimaryWordTypeCode && i.RootId == seed.RootId);
        filtered.Items.Select(i => i.Id).OrderBy(id => id).Should().Equal(expected);
    }

    // PRIMARY-vs-ANY discrimination (type): N occurs on الله and آمنوا but only as a minority —
    // a naive "any occurrence matches" predicate would return both; the primary-selection winner
    // predicate must return an EMPTY page.
    [Fact]
    public async Task PrimaryType_filter_excludes_words_where_the_code_is_only_a_minority_occurrence()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        // Precondition: both words carry N occurrences, but neither displays N as its primary type.
        var all = (await ReadAsync(handler, UniqueWordsAssociationFilter.None)).Items;
        all.Single(i => i.Id == AllahUniqueWordId).PrimaryWordTypeCode.Should().Be("PN");
        all.Single(i => i.Id == AmanuUniqueWordId).PrimaryWordTypeCode.Should().Be("V");

        var filtered = await ReadAsync(
            handler, UniqueWordsAssociationFilter.FromRaw(MinorityOnlyTypeCode, null));

        filtered.Items.Should().NotContain(i => i.Id == AllahUniqueWordId,
            "الله's N occurrence is a minority — its primary type is PN");
        filtered.Items.Should().NotContain(i => i.Id == AmanuUniqueWordId,
            "آمنوا's N occurrence is a minority — its primary type is V");
        filtered.TotalCount.Should().Be(0, "N is never a primary type anywhere in the slice");
        filtered.Items.Should().BeEmpty();
    }

    // PRIMARY-vs-ANY discrimination (root): الله co-occurs with the synthetic minority root 5003
    // but its primary root is 5001 — filtering by the minority root must EXCLUDE it (empty page),
    // while filtering by the dominant root keeps it.
    [Fact]
    public async Task PrimaryRoot_filter_excludes_word_co_occurring_with_a_minority_root()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var byMinority = await ReadAsync(
            handler, UniqueWordsAssociationFilter.FromRaw(null, MinorityCoOccurringRootId));

        byMinority.Items.Should().NotContain(i => i.Id == AllahUniqueWordId,
            "a word whose primary root differs is excluded even though it co-occurs with the filtered root");
        byMinority.TotalCount.Should().Be(0, "root 5003 is never a primary root anywhere in the slice");
        byMinority.Items.Should().BeEmpty();

        var byDominant = await ReadAsync(
            handler, UniqueWordsAssociationFilter.FromRaw(null, DominantRootOfAllah));

        byDominant.Items.Should().Contain(i => i.Id == AllahUniqueWordId);
        byDominant.Items.Should().OnlyContain(i => i.RootId == DominantRootOfAllah);
    }

    // Catalogued-but-never-primary code: ADJ exists in the POS catalogue (validation passes) but has
    // no occurrences in the slice → 200 empty page, never a 400.
    [Fact]
    public async Task Catalogued_but_never_primary_pos_code_returns_empty_page_not_error()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var page = await ReadAsync(
            handler, UniqueWordsAssociationFilter.FromRaw(CataloguedNeverOccurringTypeCode, null));

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Valid_but_unmatched_root_returns_empty_page_not_error()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var page = await ReadAsync(handler, UniqueWordsAssociationFilter.FromRaw(null, UnmatchedButValidRootId));

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Unknown_pos_code_is_rejected_with_invalid_filter()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(
                "tashkeel", null, null, 1, 1000,
                Association: UniqueWordsAssociationFilter.FromRaw("NOT_A_POS_CODE", null)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidFilter>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Nonpositive_root_id_is_rejected_with_invalid_filter(int rootId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery(
                "tashkeel", null, null, 1, 1000,
                Association: UniqueWordsAssociationFilter.FromRaw(null, rootId)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.InvalidFilter>();
    }

    private static async Task<PagedResult<UniqueWordListItemDto>> ReadAsync(
        GetUniqueWordsPageHandler handler, UniqueWordsAssociationFilter association) =>
        (await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 1000, Association: association),
            CancellationToken.None))
        .Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
}
