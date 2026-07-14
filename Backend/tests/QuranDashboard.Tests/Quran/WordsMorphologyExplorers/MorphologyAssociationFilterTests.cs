using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

// Feature 026, US7 — association filters on the Lemmas and Stems lists. Lemmas filter by the real
// owned-root FK (quran_lemmas.root_id); Stems filter by the derived PRIMARY (dominant) root/lemma
// association surfaced on their list rows. The primary-not-sole semantics are pinned from the seed's
// S602 'عَلِمَ' (dominant root R701, but it also co-occurs with R702 once).
[Collection(nameof(MorphologyExplorersCollection))]
public sealed class MorphologyAssociationFilterTests(MorphologyExplorersTestFixture fixture)
{
    private const int OwnedRootWithLemmas = 701;   // R701 'ع ل م' — owned by lemmas L502 + L504
    private const int CoOccurringNonDominantRoot = 702; // R702 'ك ت ب' — co-occurs on S602 but is not its primary
    private const int MultiCandidateStemId = 602;  // S602 'عَلِمَ' — dominant root R701, dominant lemma L502
    private const int DominantLemmaOfS602 = 502;   // L502 'عِلْم'
    private const int UnmatchedButValidId = 999_999;

    // -- Lemmas: real FK belonging ------------------------------------------------------------------

    [Fact]
    public async Task GetLemmasPage_filters_by_owned_root_fk_and_agrees_with_displayed_root()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var all = (await ReadLemmasAsync(handler, LemmasAssociationFilter.None)).Items;
        var expected = all.Where(l => l.RootId == OwnedRootWithLemmas).Select(l => l.Id).OrderBy(id => id).ToList();
        expected.Should().NotBeEmpty("the seed owns L502 and L504 by R701");

        var filtered = await ReadLemmasAsync(handler, LemmasAssociationFilter.FromRaw(OwnedRootWithLemmas));

        filtered.Items.Select(l => l.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.Items.Should().OnlyContain(l => l.RootId == OwnedRootWithLemmas,
            "every filtered row's displayed owned root equals the filter (the chip⇔filter invariant)");
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task GetLemmasPage_valid_but_unmatched_root_returns_empty_page_not_error()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var page = await ReadLemmasAsync(handler, LemmasAssociationFilter.FromRaw(UnmatchedButValidId));

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task GetLemmasPage_nonpositive_root_returns_invalid_filter(int rootId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50, Association: LemmasAssociationFilter.FromRaw(rootId)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmasPageOutcome.InvalidFilter>();
    }

    // -- Stems: primary (dominant) association, primary-not-sole ------------------------------------

    [Fact]
    public async Task GetStemsPage_filters_by_primary_root_and_agrees_with_displayed_dominant_root()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var all = (await ReadStemsAsync(handler, StemsAssociationFilter.None)).Items;
        var expected = all.Where(s => s.RootId == OwnedRootWithLemmas).Select(s => s.Id).OrderBy(id => id).ToList();
        expected.Should().Contain(MultiCandidateStemId, "S602's dominant root is R701");

        var filtered = await ReadStemsAsync(handler, StemsAssociationFilter.FromRaw(OwnedRootWithLemmas, null));

        filtered.Items.Select(s => s.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.Items.Should().OnlyContain(s => s.RootId == OwnedRootWithLemmas,
            "every filtered row's displayed primary root equals the filter");
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task GetStemsPage_primary_root_filter_excludes_co_occurring_non_dominant_stem()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        // S602 co-occurs with R702 (once) but its PRIMARY root is R701; filtering by the co-occurring,
        // non-dominant root must exclude it — this is the primary-not-sole contract.
        var filtered = await ReadStemsAsync(handler, StemsAssociationFilter.FromRaw(CoOccurringNonDominantRoot, null));

        filtered.Items.Should().NotContain(s => s.Id == MultiCandidateStemId,
            "a stem whose primary root differs is excluded even though it co-occurs with the filtered root");
        filtered.Items.Should().OnlyContain(s => s.RootId == CoOccurringNonDominantRoot);
    }

    [Fact]
    public async Task GetStemsPage_filters_by_primary_lemma_and_agrees_with_displayed_dominant_lemma()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var all = (await ReadStemsAsync(handler, StemsAssociationFilter.None)).Items;
        var expected = all.Where(s => s.LemmaId == DominantLemmaOfS602).Select(s => s.Id).OrderBy(id => id).ToList();
        expected.Should().Contain(MultiCandidateStemId, "S602's dominant lemma is L502");

        var filtered = await ReadStemsAsync(handler, StemsAssociationFilter.FromRaw(null, DominantLemmaOfS602));

        filtered.Items.Select(s => s.Id).OrderBy(id => id).Should().Equal(expected);
        filtered.Items.Should().OnlyContain(s => s.LemmaId == DominantLemmaOfS602);
        filtered.TotalCount.Should().Be(expected.Count);
    }

    [Fact]
    public async Task GetStemsPage_root_and_lemma_compose_as_intersection()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var filtered = await ReadStemsAsync(
            handler, StemsAssociationFilter.FromRaw(OwnedRootWithLemmas, DominantLemmaOfS602));

        filtered.Items.Should().OnlyContain(
            s => s.RootId == OwnedRootWithLemmas && s.LemmaId == DominantLemmaOfS602);
        filtered.Items.Should().Contain(s => s.Id == MultiCandidateStemId);
    }

    [Fact]
    public async Task GetStemsPage_valid_but_unmatched_id_returns_empty_page_not_error()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var page = await ReadStemsAsync(handler, StemsAssociationFilter.FromRaw(UnmatchedButValidId, null));

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(null, 0)]
    [InlineData(null, -2)]
    public async Task GetStemsPage_nonpositive_id_returns_invalid_filter(int? rootId, int? lemmaId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50, Association: StemsAssociationFilter.FromRaw(rootId, lemmaId)),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemsPageOutcome.InvalidFilter>();
    }

    private static async Task<PagedResult<LemmaListItemDto>> ReadLemmasAsync(
        GetLemmasPageHandler handler, LemmasAssociationFilter association) =>
        (await handler.HandleAsync(
            new GetLemmasPageQuery(null, null, 1, 50, Association: association),
            CancellationToken.None))
        .Should().BeOfType<GetLemmasPageOutcome.Success>().Subject.Page;

    private static async Task<PagedResult<StemListItemDto>> ReadStemsAsync(
        GetStemsPageHandler handler, StemsAssociationFilter association) =>
        (await handler.HandleAsync(
            new GetStemsPageQuery(null, null, 1, 50, Association: association),
            CancellationToken.None))
        .Should().BeOfType<GetStemsPageOutcome.Success>().Subject.Page;
}
