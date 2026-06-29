using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;
using QuranDashboard.Infrastructure.Caching.Quran.Words.Stems;
using QuranDashboard.Infrastructure.Persistence;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;
using QuranDashboard.Tests.Quran.Words;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsWordsReadTests(MorphologyExplorersTestFixture fixture)
{
    private const int HighFrequencyStemId = 600;
    private const int SegmentFanoutStemId = 606;
    private const int UnknownStemId = 999_999;

    [Theory]
    [InlineData(StemWordKindKeys.Simple, 32001, "كلمة", 10, 32002, "كلم", 1)]
    [InlineData(StemWordKindKeys.Tashkeel, 31001, "كَلِمَة", 10, 31002, "كَلَّمَ", 1)]
    public async Task GetStemWords_returns_correct_unique_ids_display_text_and_counts_for_each_kind(
        string kind,
        int firstUniqueWordId,
        string firstDisplayText,
        int firstOccurrencesCount,
        int secondUniqueWordId,
        string secondDisplayText,
        int secondOccurrencesCount)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemWordsQuery(HighFrequencyStemId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemWordsOutcome.Success>().Subject.Page;
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
    [InlineData(StemWordKindKeys.Simple, 32010, "لفظ-تجريبي", 2, 32008, "الا", 1, 32009, "لا", 1)]
    [InlineData(StemWordKindKeys.Tashkeel, 31010, "لَفْظٌ-تَجْرِيبِيّ", 2, 31008, "أَلَّا", 1, 31009, "لَا", 1)]
    public async Task GetStemWords_counts_segment_fanout_and_secondary_membership_by_unique_word(
        string kind,
        int firstUniqueWordId,
        string firstDisplayText,
        int firstOccurrencesCount,
        int secondUniqueWordId,
        string secondDisplayText,
        int secondOccurrencesCount,
        int thirdUniqueWordId,
        string thirdDisplayText,
        int thirdOccurrencesCount)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemWordsQuery(SegmentFanoutStemId, kind, 1, 50),
            CancellationToken.None);

        var page = outcome.Should().BeOfType<GetStemWordsOutcome.Success>().Subject.Page;
        page.TotalCount.Should().Be(3);
        page.Items.Select(i => i.UniqueWordId).Should().Equal(firstUniqueWordId, secondUniqueWordId, thirdUniqueWordId);

        page.Items[0].DisplayText.Should().Be(firstDisplayText);
        page.Items[0].OccurrencesCount.Should().Be(firstOccurrencesCount);

        page.Items[1].DisplayText.Should().Be(secondDisplayText);
        page.Items[1].OccurrencesCount.Should().Be(secondOccurrencesCount);

        page.Items[2].DisplayText.Should().Be(thirdDisplayText);
        page.Items[2].OccurrencesCount.Should().Be(thirdOccurrencesCount);
    }

    [Theory]
    [InlineData(StemWordKindKeys.Simple, 32001, 32002)]
    [InlineData(StemWordKindKeys.Tashkeel, 31001, 31002)]
    public async Task GetStemWords_pages_the_results_by_unique_word_identity(
        string kind,
        int firstUniqueWordId,
        int secondUniqueWordId)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var first = await handler.HandleAsync(
            new GetStemWordsQuery(HighFrequencyStemId, kind, 1, 1),
            CancellationToken.None);
        var firstPage = first.Should().BeOfType<GetStemWordsOutcome.Success>().Subject.Page;
        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Should().ContainSingle(i => i.UniqueWordId == firstUniqueWordId);

        var second = await handler.HandleAsync(
            new GetStemWordsQuery(HighFrequencyStemId, kind, 2, 1),
            CancellationToken.None);
        var secondPage = second.Should().BeOfType<GetStemWordsOutcome.Success>().Subject.Page;
        secondPage.TotalCount.Should().Be(2);
        secondPage.Items.Should().ContainSingle(i => i.UniqueWordId == secondUniqueWordId);
    }

    [Theory]
    [InlineData(StemWordKindKeys.Simple)]
    [InlineData(StemWordKindKeys.Tashkeel)]
    public async Task GetStemWords_huge_positive_page_returns_empty_without_skip_overflow(string kind)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemWordsQuery(HighFrequencyStemId, kind, int.MaxValue, 1000),
            CancellationToken.None);
        var page = outcome.Should().BeOfType<GetStemWordsOutcome.Success>().Subject.Page;

        page.Page.Should().Be(int.MaxValue);
        page.TotalCount.Should().Be(2);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStemWords_invalid_kind_returns_invalid_kind()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemWordsQuery(HighFrequencyStemId, "invalid", 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemWordsOutcome.InvalidKind>();
    }

    [Fact]
    public async Task GetStemWords_unknown_id_returns_not_found()
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemWordsHandler>();

        var outcome = await handler.HandleAsync(
            new GetStemWordsQuery(UnknownStemId, StemWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetStemWordsOutcome.NotFound>();
    }

    [Theory]
    [InlineData(StemWordKindKeys.Simple)]
    [InlineData(StemWordKindKeys.Tashkeel)]
    public async Task GetStemWords_repeated_read_issues_no_new_db_commands_after_cache(string kind)
    {
        var interceptor = new SqlCommandCountInterceptor();
        var options = new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        await using var dbContext = new QuranDashboardDbContext(options);
        var inner = new EfStemsReader(dbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var reader = new CachedStemsReader(inner, cache);

        await reader.GetStemWordsAsync(
            HighFrequencyStemId,
            kind == StemWordKindKeys.Simple ? StemWordKind.Simple : StemWordKind.Tashkeel,
            1,
            50,
            CancellationToken.None);

        interceptor.Reset();

        await reader.GetStemWordsAsync(
            HighFrequencyStemId,
            kind == StemWordKindKeys.Simple ? StemWordKind.Simple : StemWordKind.Tashkeel,
            1,
            50,
            CancellationToken.None);

        interceptor.CommandCount.Should().Be(0);
    }
}
