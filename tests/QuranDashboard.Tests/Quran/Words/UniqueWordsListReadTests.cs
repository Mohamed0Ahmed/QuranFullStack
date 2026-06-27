using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsListReadTests(UniqueWordsTestFixture fixture)
{
    private const int SeededUniqueWordsPerKind = 8;

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

        foreach (var item in page.Items)
        {
            item.OccurrencesCount.Should().BeGreaterThan(0);
            item.AyahsCount.Should().BeGreaterThan(0);
            item.SurahsCount.Should().BeInRange(1, 114);
            item.MissingSurahsCount.Should().Be(114 - item.SurahsCount);
        }

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
    public async Task GetUniqueWordsPage_returns_display_text_for_each_kind_mode()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var tashkeelOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var tashkeelPage = tashkeelOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var allah = tashkeelPage.Items.Single(i => i.Id == 1002);
        allah.DisplayText.Should().Be("ٱللَّهِ");

        var simpleOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("simple", null, null, 1, 50),
            CancellationToken.None);
        var simplePage = simpleOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var amanu = simplePage.Items.Single(i => i.Id == 2003);
        amanu.DisplayText.Should().Be("ءامنوا");
    }

    [Fact]
    public async Task GetUniqueWordsPage_returns_primary_word_type_and_root_for_word_with_morphology()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var tashkeelOutcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var page = tashkeelOutcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var allah = page.Items.Single(i => i.Id == 1002);
        allah.PrimaryWordTypeCode.Should().Be("PN");
        allah.PrimaryWordTypeBroadArabicLabel.Should().Be("اسم");
        allah.RootId.Should().Be(5001);
        allah.RootText.Should().Be("أ ل ه");

        var amanu = page.Items.Single(i => i.Id == 2003);
        amanu.PrimaryWordTypeCode.Should().Be("V");
        amanu.PrimaryWordTypeBroadArabicLabel.Should().Be("فعل");
        amanu.RootId.Should().Be(5002);
        amanu.RootText.Should().Be("أ م ن");

        var particle = page.Items.Single(i => i.Id == 1202);
        particle.PrimaryWordTypeCode.Should().Be("P");
        particle.PrimaryWordTypeBroadArabicLabel.Should().Be("حرف");
        particle.RootId.Should().BeNull();
        particle.RootText.Should().BeNull();

        var initials = page.Items.Single(i => i.Id == 31001);
        initials.PrimaryWordTypeCode.Should().Be("INL");
        initials.PrimaryWordTypeBroadArabicLabel.Should().Be("حروف مقطّعة");
        initials.RootId.Should().BeNull();
        initials.RootText.Should().BeNull();
    }

    [Fact]
    public async Task GetUniqueWordsPage_resolves_prohibition_particle_as_particle_broad_label()
    {
        await using var scope = fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        await SeedProhibitionParticleUniqueWordAsync(dbContext);

        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();
        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "لا", null, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;
        var prohibitionParticle = page.Items.Single(i => i.Id == 2007);

        prohibitionParticle.PrimaryWordTypeCode.Should().Be("PRO");
        prohibitionParticle.PrimaryWordTypeBroadArabicLabel.Should().Be("حرف");
        prohibitionParticle.PrimaryWordTypeBroadArabicLabel.Should().NotBe("اسم");
    }

    [Fact]
    public async Task GetUniqueWordsPage_returns_null_type_and_root_when_morphology_absent()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>().Subject.Page;

        var bism = page.Items.Single(i => i.Id == 1001);
        bism.PrimaryWordTypeCode.Should().BeNull();
        bism.PrimaryWordTypeBroadArabicLabel.Should().BeNull();
        bism.RootId.Should().BeNull();
        bism.RootText.Should().BeNull();
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

    private static async Task SeedProhibitionParticleUniqueWordAsync(QuranDashboardDbContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO quran_ayahs
              (id, surah_number, ayah_number, verse_key, text_uthmani, words_count_source, words_count_real, page_from, page_to, juz_number, hizb_number, rub_number)
            VALUES
              (26, 2, 26, '2:26', 'ٱللَّهُ لَا إِلَٰهَ إِلَّا هُوَ ٱلْحَىُّ ٱلْقَيُّومُ', 4, 5, 5, 5, NULL, NULL, NULL)
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO quran_words
              (id, location, ayah_id, surah_number, ayah_number, word_number, page_number, line_number, line_word_order, qpc_glyph,
               text_uthmani, text_uthmani_simple, text_imlaei_simple, word_key_imlaei_simple,
               is_ayah_marker, unique_tashkeel_word_id, unique_simple_word_id)
            VALUES
              (2007, '2:26:2', 26, 2, 26, 2, 5, 2, 2, 'g2007', 'لَا', 'لا', 'لا', 'لا', FALSE, NULL, NULL);

            INSERT INTO quran_words_unique_tashkeel
              (id, text_uthmani, text_uthmani_simple, text_imlaei_simple,
               occurrences_count, ayahs_count, surahs_count,
               first_quran_word_id, first_location, first_surah_number, first_ayah_number,
               first_word_order_in_mushaf, first_page_number, first_line_number)
            VALUES
              (2007, 'لَا', 'لا', 'لا', 1, 1, 1, 2007, '2:26:2', 2, 26, 2007, 5, 2);

            INSERT INTO quran_words_unique_simple
              (id, word_key_imlaei_simple, text_uthmani, text_uthmani_simple, text_imlaei_simple, qpc_glyph,
               occurrences_count, ayahs_count, surahs_count,
               first_quran_word_id, first_location, first_surah_number, first_ayah_number,
               first_word_order_in_mushaf, first_page_number, first_line_number)
            VALUES
              (2007, 'لا', 'لَا', 'لا', 'لا', 'g2007', 1, 1, 1, 2007, '2:26:2', 2, 26, 2007, 5, 2);

            UPDATE quran_words
            SET unique_tashkeel_word_id = 2007,
                unique_simple_word_id = 2007
            WHERE id = 2007;

            INSERT INTO quran_pos_tags
              (code, arabic_label, english_label, category, sort_order, description)
            VALUES
              ('PRO', 'حرف نهي', 'Prohibition Particle', 'particle', 20, NULL);

            INSERT INTO quran_word_morphology
              (quran_word_id, location, head_pos, segment_count, root_id, lemma_id, stem_id, is_verb, verb_tense, verb_voice, case_feature, head_features_json)
            VALUES
              (2007, '2:26:2', 'PRO', 1, NULL, NULL, NULL, FALSE, NULL, NULL, NULL, NULL);
            """);
    }
}
