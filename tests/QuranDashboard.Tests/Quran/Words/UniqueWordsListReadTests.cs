using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

/// <summary>
/// US2 default-list read behavior: page metadata, total count, the four
/// distribution counts, <c>missingSurahsCount = 114 - surahsCount</c>,
/// simple-mode Uthmani display label, and <c>firstVerseKey</c> derivation.
/// </summary>
[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsListReadTests(UniqueWordsTestFixture fixture)
{
    private const int SeededUniqueWordsPerKind = 6;

    [Fact]
    public async Task GetUniqueWordsPage_returns_default_page_metadata_for_tashkeel()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.TotalCount.Should().Be(SeededUniqueWordsPerKind);
        page.Items.Should().HaveCount(SeededUniqueWordsPerKind);
    }

    [Fact]
    public async Task GetUniqueWordsPage_returns_all_four_counts_and_missing_surahs_invariant()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        // Every card must show occurrences/ayahs/surahs counts and the derived
        // missing-surahs invariant (SC-004): 114 - surahsCount.
        foreach (var item in page.Items)
        {
            item.OccurrencesCount.Should().BeGreaterThan(0);
            item.AyahsCount.Should().BeGreaterThan(0);
            item.SurahsCount.Should().BeInRange(1, 114);
            item.MissingSurahsCount.Should().Be(114 - item.SurahsCount);
        }

        // The seeded high-occurrence word (الله) pins the invariant at a
        // surahsCount of 5 → missingSurahsCount 109.
        var allah = page.Items.Single(i => i.Id == 1002);
        allah.OccurrencesCount.Should().Be(5);
        allah.AyahsCount.Should().Be(5);
        allah.SurahsCount.Should().Be(5);
        allah.MissingSurahsCount.Should().Be(109);
    }

    [Fact]
    public async Task GetUniqueWordsPage_uses_kind_key_string_in_items()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var tashkeelOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var tashkeelPage = tashkeelOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        tashkeelPage.Items.Should().OnlyContain(i => i.Kind == UniqueWordKindKeys.Tashkeel);

        var simpleOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", null, null, 1, 50),
            CancellationToken.None);
        var simplePage = simpleOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        simplePage.Items.Should().OnlyContain(i => i.Kind == UniqueWordKindKeys.Simple);
    }

    [Fact]
    public async Task GetUniqueWordsPage_derives_first_verse_key_from_first_surah_and_ayah()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var amanu = page.Items.Single(i => i.Id == 2003);
        amanu.FirstVerseKey.Should().Be("2:25");
        amanu.FirstLocation.Should().Be("2:25:3");
    }

    [Fact]
    public async Task GetUniqueWordsPage_exposes_raw_word_forms_and_preserves_display_text()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var tashkeelOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);

        var tashkeelPage = tashkeelOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var allah = tashkeelPage.Items.Single(i => i.Id == 1002);
        allah.DisplayTextUthmani.Should().Be("ٱللَّهِ");
        allah.TextUthmani.Should().Be("ٱللَّهِ");
        allah.TextUthmaniSimple.Should().Be("الله");
        allah.TextImlaeiSimple.Should().Be("الله");
        allah.WordKeyImlaeiSimple.Should().BeNull();
        allah.QpcGlyph.Should().BeNull();

        var simpleOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", null, null, 1, 50),
            CancellationToken.None);

        var page = simpleOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var amanu = page.Items.Single(i => i.Id == 2003);
        amanu.DisplayTextUthmani.Should().Be("ءَامَنُوا۟");
        amanu.TextUthmani.Should().Be("ءَامَنُوا۟");
        amanu.TextUthmaniSimple.Should().Be("ءامنوا");
        amanu.TextImlaeiSimple.Should().Be("آمنوا");
        amanu.WordKeyImlaeiSimple.Should().Be("امنوا");
        amanu.QpcGlyph.Should().Be("g2003");
    }

    [Fact]
    public async Task GetUniqueWordsPage_default_sort_is_mushaf_order()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        page.Items.Select(i => i.Id).Should().BeInAscendingOrder();
    }
}
