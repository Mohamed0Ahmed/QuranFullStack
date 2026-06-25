using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class StemsLoggingTests(MorphologyExplorersTestFixture fixture)
{
    private const int StemId = 602;
    private const int NullRelationStemId = 601;
    private const int UnknownStemId = 999_999;
    private const string SearchText = "حكم";

    [Fact]
    public async Task GetStemsPage_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(new GetStemsPageQuery(null, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Stems");
        entry.GetValue<string>("operation").Should().Be("GetStemsPage");
        entry.GetValue<string>("sort").Should().Be(StemSortKeys.MushafOrder);
        entry.GetValue<int>("pageNumber").Should().Be(1);
        entry.GetValue<int>("pageSize").Should().Be(50);
        entry.GetValue<int>("totalCount").Should().Be(5);
        entry.GetValue<int>("itemCount").Should().Be(5);
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
    }

    [Fact]
    public async Task GetStemsPage_with_search_logs_hasSearch_without_raw_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(new GetStemsPageQuery(SearchText, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.FieldNames().Should().NotContain("search");
        AssertNoText(entry, SearchText);
    }

    [Theory]
    [MemberData(nameof(GetStemsPageWarningCases))]
    public async Task GetStemsPage_logs_one_warning_for_controlled_refusals(
        GetStemsPageQuery query,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemsPageHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetStemsPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        AssertNoText(entry, SearchText);
    }

    [Fact]
    public async Task GetStemSummary_logs_success_with_safe_counts_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        var outcome = await handler.HandleAsync(new GetStemSummaryQuery(StemId), CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetStemSummaryOutcome.Success>().Subject.Summary;

        var entry = SingleEntryFor<GetStemSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "stemId", "dominantLemmaId", "dominantRootId", "dominantTypeCode", "otherTypesCount", "occurrencesCount", "ayahsCount", "surahsCount", "simpleWordsCount", "tashkeelWordsCount"]);
        entry.GetValue<int>("stemId").Should().Be(StemId);
        entry.GetValue<int?>("dominantLemmaId").Should().Be(502);
        entry.GetValue<int?>("dominantRootId").Should().Be(701);
        entry.GetValue<string>("dominantTypeCode").Should().Be("V");
        entry.GetValue<int>("otherTypesCount").Should().Be(0);
        entry.GetValue<int>("occurrencesCount").Should().Be(summary.OccurrencesCount);
        entry.GetValue<int>("ayahsCount").Should().Be(summary.AyahsCount);
        entry.GetValue<int>("surahsCount").Should().Be(summary.SurahsCount);
        entry.GetValue<int>("simpleWordsCount").Should().Be(summary.SimpleWordsCount);
        entry.GetValue<int>("tashkeelWordsCount").Should().Be(summary.TashkeelWordsCount);
        AssertNoText(entry, summary.StemText);
    }

    [Fact]
    public async Task GetStemSummary_logs_null_relationships_without_raw_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        await handler.HandleAsync(new GetStemSummaryQuery(NullRelationStemId), CancellationToken.None);

        var entry = SingleEntryFor<GetStemSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().Contain(new[] { "stemId", "dominantLemmaId", "dominantRootId" });
        entry.StructuredFields().Single(pair => pair.Key == "dominantLemmaId").Value.Should().BeNull();
        entry.StructuredFields().Single(pair => pair.Key == "dominantRootId").Value.Should().BeNull();
        AssertNoText(entry, "مَجْهُول");
    }

    [Theory]
    [MemberData(nameof(GetStemSummaryWarningCases))]
    public async Task GetStemSummary_logs_one_warning_for_controlled_failures(
        GetStemSummaryQuery query,
        LogLevel expectedLevel,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetStemSummaryHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetStemSummaryHandler>(expectedLevel);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<int>("stemId").Should().Be(query.Id);
    }

    public static TheoryData<GetStemsPageQuery, string, string[]> GetStemsPageWarningCases => new()
    {
        {
            new GetStemsPageQuery(SearchText, "bogus", 1, 50), "invalidSort",
            ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"]
        },
        {
            new GetStemsPageQuery(SearchText, null, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "sort", "pageNumber", "pageSize", "hasSearch"]
        },
    };

    public static TheoryData<GetStemSummaryQuery, LogLevel, string[]> GetStemSummaryWarningCases => new()
    {
        { new GetStemSummaryQuery(0), LogLevel.Warning, ["feature", "operation", "reason", "stemId"] },
        { new GetStemSummaryQuery(UnknownStemId), LogLevel.Warning, ["feature", "operation", "stemId"] },
    };

    private RecordingLoggerProvider.LogEntry SingleEntryFor<THandler>(LogLevel expectedLevel)
    {
        var categoryName = typeof(THandler).FullName;
        var entry = fixture.LoggingProvider.Entries
            .Where(e => e.CategoryName == categoryName)
            .Should()
            .ContainSingle()
            .Subject;
        entry.Level.Should().Be(expectedLevel);
        return entry;
    }

    private static void AssertNoText(RecordingLoggerProvider.LogEntry entry, string text)
    {
        entry.Message.Should().NotContain(text);
        entry.StructuredFieldsWithoutOriginalFormat()
            .Select(pair => pair.Value?.ToString())
            .Should()
            .NotContain(text);
    }
}
