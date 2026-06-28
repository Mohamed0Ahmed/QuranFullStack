using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Lemmas;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Lemmas;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasWordsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyLemmaId = 500;
    private const int CompoundLemmaAnId = 507;
    private const int SameLemmaFanoutLemmaId = 508;
    private const int MarkerRegressionLemmaId = 509;
    private const int UnknownLemmaId = 999_999;

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple, 32001, "كلمة", 10, 32002, "كلم", 1)]
    [InlineData(LemmaWordKindKeys.Tashkeel, 31001, "كَلِمَة", 10, 31002, "كَلَّمَ", 1)]
    public async Task GetLemmaWords_returns_correct_unique_ids_display_text_and_counts_for_each_kind(
        string kind,
        int firstUniqueWordId,
        string firstDisplayText,
        int firstOccurrencesCount,
        int secondUniqueWordId,
        string secondDisplayText,
        int secondOccurrencesCount)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(HighFrequencyLemmaId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(2);
        page.Items.Select(i => i.UniqueWordId).Should().Equal(firstUniqueWordId, secondUniqueWordId);

        var first = page.Items[0];
        first.DisplayText.Should().Be(firstDisplayText);
        first.OccurrencesCount.Should().Be(firstOccurrencesCount);

        var second = page.Items[1];
        second.DisplayText.Should().Be(secondDisplayText);
        second.OccurrencesCount.Should().Be(secondOccurrencesCount);
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple, 32008, "الا")]
    [InlineData(LemmaWordKindKeys.Tashkeel, 31008, "أَلَّا")]
    public async Task GetLemmaWords_compound_segment_only_lemma_includes_matched_word(
        string kind,
        int uniqueWordId,
        string displayText)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(CompoundLemmaAnId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].UniqueWordId.Should().Be(uniqueWordId);
        page.Items[0].DisplayText.Should().Be(displayText);
        page.Items[0].OccurrencesCount.Should().Be(1);
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple, 32010, "لفظ-تجريبي")]
    [InlineData(LemmaWordKindKeys.Tashkeel, 31010, "لَفْظٌ-تَجْرِيبِيّ")]
    public async Task GetLemmaWords_same_lemma_segment_fanout_groups_unique_word_once_and_counts_segments(
        string kind,
        int uniqueWordId,
        string displayText)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();
        var summaryHandler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(SameLemmaFanoutLemmaId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].UniqueWordId.Should().Be(uniqueWordId);
        page.Items[0].DisplayText.Should().Be(displayText);
        page.Items[0].OccurrencesCount.Should().Be(2);

        var summary = (await summaryHandler.HandleAsync(
            new GetLemmaSummaryQuery(SameLemmaFanoutLemmaId),
            CancellationToken.None)).Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;
        page.Items.Sum(i => i.OccurrencesCount).Should().Be(summary.OccurrencesCount);
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple, 32011, "وسم-تجريبي", 32012)]
    [InlineData(LemmaWordKindKeys.Tashkeel, 31011, "وَسْم-تَجْرِيبِيّ", 31012)]
    public async Task GetLemmaWords_matched_ayah_marker_returns_only_real_word(
        string kind,
        int realUniqueWordId,
        string realDisplayText,
        int markerUniqueWordId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(MarkerRegressionLemmaId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.Items[0].UniqueWordId.Should().Be(realUniqueWordId);
        page.Items[0].DisplayText.Should().Be(realDisplayText);
        page.Items[0].OccurrencesCount.Should().Be(1);
        page.Items.Select(i => i.UniqueWordId).Should().NotContain(markerUniqueWordId);
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple, 32001, 32002)]
    [InlineData(LemmaWordKindKeys.Tashkeel, 31001, 31002)]
    public async Task GetLemmaWords_pages_the_results_by_unique_word_identity(
        string kind,
        int firstUniqueWordId,
        int secondUniqueWordId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var first = await handler.HandleAsync(
            new GetLemmaWordsQuery(HighFrequencyLemmaId, kind, 1, 1),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Should().ContainSingle(i => i.UniqueWordId == firstUniqueWordId);

        var second = await handler.HandleAsync(
            new GetLemmaWordsQuery(HighFrequencyLemmaId, kind, 2, 1),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;
        secondPage.TotalCount.Should().Be(2);
        secondPage.Items.Should().ContainSingle(i => i.UniqueWordId == secondUniqueWordId);
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple)]
    [InlineData(LemmaWordKindKeys.Tashkeel)]
    public async Task GetLemmaWords_huge_positive_page_returns_empty_without_skip_overflow(string kind)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(HighFrequencyLemmaId, kind, int.MaxValue, 1000),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetLemmaWordsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(2);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLemmaWords_invalid_kind_returns_invalid_kind()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(HighFrequencyLemmaId, "invalid", 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaWordsOutcome.InvalidKind>();
    }

    [Fact]
    public async Task GetLemmaWords_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetLemmaWordsQuery(UnknownLemmaId, LemmaWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetLemmaWordsOutcome.NotFound>();
    }

    [Theory]
    [InlineData(LemmaWordKindKeys.Simple)]
    [InlineData(LemmaWordKindKeys.Tashkeel)]
    public async Task GetLemmaWords_repeated_read_issues_no_new_db_commands_after_cache(string kind)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfLemmasReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedLemmasReader(inner, cache);

        await reader.GetLemmaWordsAsync(
            HighFrequencyLemmaId,
            kind == LemmaWordKindKeys.Simple ? LemmaWordKind.Simple : LemmaWordKind.Tashkeel,
            1,
            50,
            CancellationToken.None);

        interceptor.Reset();

        await reader.GetLemmaWordsAsync(
            HighFrequencyLemmaId,
            kind == LemmaWordKindKeys.Simple ? LemmaWordKind.Simple : LemmaWordKind.Tashkeel,
            1,
            50,
            CancellationToken.None);

        interceptor.CommandCount.Should().Be(0);
    }
}
