using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;

namespace QuranDashboard.Tests.Quran.WordsRoots;

[Collection(nameof(RootsExplorerCollection))]
public sealed class RootsLoggingTests(RootsExplorerTestFixture fixture)
{
    private const int RootId = 10;
    private const int UnknownRootId = 999_999;
    private const string SearchText = "رحم";

    [Fact]
    public async Task GetRootsPage_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        await handler.HandleAsync(new GetRootsPageQuery(null, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetRootsPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Roots");
        entry.GetValue<string>("operation").Should().Be("GetRootsPage");
        entry.GetValue<string>("sort").Should().Be(RootSortKeys.MushafOrder);
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
    }

    [Fact]
    public async Task GetRootsPage_with_search_logs_hasSearch_without_raw_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        await handler.HandleAsync(new GetRootsPageQuery(SearchText, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetRootsPageHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.FieldNames().Should().NotContain("search");
        AssertNoText(entry, SearchText);
    }

    [Theory]
    [MemberData(nameof(GetRootsPageWarningCases))]
    public async Task GetRootsPage_logs_one_warning_for_controlled_refusals(
        GetRootsPageQuery query,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootsPageHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetRootsPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        AssertNoText(entry, SearchText);
    }

    [Fact]
    public async Task GetRootSummary_logs_success_with_safe_counts_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootSummaryHandler>();

        var outcome = await handler.HandleAsync(new GetRootSummaryQuery(RootId), CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetRootSummaryOutcome.Success>().Subject.Summary;

        var entry = SingleEntryFor<GetRootSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "rootId", "occurrencesCount", "ayahsCount", "surahsCount", "lemmasCount", "stemsCount"]);
        entry.GetValue<int>("rootId").Should().Be(RootId);
        entry.GetValue<int>("occurrencesCount").Should().Be(summary.OccurrencesCount);
        entry.GetValue<int>("lemmasCount").Should().Be(summary.LemmasCount);
    }

    [Theory]
    [MemberData(nameof(GetRootSummaryWarningCases))]
    public async Task GetRootSummary_logs_one_warning_for_controlled_failures(
        GetRootSummaryQuery query,
        LogLevel expectedLevel,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootSummaryHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetRootSummaryHandler>(expectedLevel);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<int>("rootId").Should().Be(query.Id);
    }

    [Fact]
    public async Task GetRootAyahs_logs_success_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootAyahsHandler>();

        await handler.HandleAsync(new GetRootAyahsQuery(RootId, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetRootAyahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "rootId", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("ayahs");
        entry.GetValue<int>("rootId").Should().Be(RootId);
    }

    [Fact]
    public async Task GetRootWords_logs_success_with_subView()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        await handler.HandleAsync(
            new GetRootWordsQuery(RootId, RootWordKindKeys.Simple, 1, 50),
            CancellationToken.None);

        var entry = SingleEntryFor<GetRootWordsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "rootId", "subView", "pageNumber", "pageSize", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("words");
        entry.GetValue<string>("subView").Should().Be(RootWordKindKeys.Simple);
    }

    [Fact]
    public async Task GetRootWords_invalid_kind_logs_warning_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootWordsHandler>();

        await handler.HandleAsync(new GetRootWordsQuery(RootId, "not-a-kind", 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetRootWordsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "reason", "view", "rootId", "subView", "pageNumber", "pageSize"]);
        entry.GetValue<string>("reason").Should().Be("invalidKind");
    }

    [Fact]
    public async Task GetRootMentionedSurahs_logs_success_with_subView()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootMentionedSurahsHandler>();

        await handler.HandleAsync(new GetRootMentionedSurahsQuery(RootId), CancellationToken.None);

        var entry = SingleEntryFor<GetRootMentionedSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "rootId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("surahs");
        entry.GetValue<string>("subView").Should().Be("mentioned");
    }

    [Fact]
    public async Task GetRootMissingSurahs_logs_success_with_subView()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootMissingSurahsHandler>();

        await handler.HandleAsync(new GetRootMissingSurahsQuery(RootId), CancellationToken.None);

        var entry = SingleEntryFor<GetRootMissingSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "subView", "rootId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("subView").Should().Be("missing");
    }

    [Fact]
    public async Task GetRootLemmas_logs_success_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootLemmasHandler>();

        await handler.HandleAsync(new GetRootLemmasQuery(RootId), CancellationToken.None);

        var entry = SingleEntryFor<GetRootLemmasHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "rootId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("lemmas");
    }

    [Fact]
    public async Task GetRootStems_logs_success_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetRootStemsHandler>();

        await handler.HandleAsync(new GetRootStemsQuery(RootId), CancellationToken.None);

        var entry = SingleEntryFor<GetRootStemsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "view", "rootId", "totalCount", "itemCount", "elapsedMs"]);
        entry.GetValue<string>("view").Should().Be("stems");
    }

    [Fact]
    public async Task No_handler_logs_quran_or_root_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var sp = scope.ServiceProvider;

        var summaryOutcome = await sp.GetRequiredService<GetRootSummaryHandler>()
            .HandleAsync(new GetRootSummaryQuery(RootId), CancellationToken.None);
        var rootText = summaryOutcome.Should().BeOfType<GetRootSummaryOutcome.Success>().Subject.Summary.RootText;

        await sp.GetRequiredService<GetRootsPageHandler>()
            .HandleAsync(new GetRootsPageQuery(SearchText, null, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetRootAyahsHandler>()
            .HandleAsync(new GetRootAyahsQuery(RootId, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetRootWordsHandler>()
            .HandleAsync(new GetRootWordsQuery(RootId, RootWordKindKeys.Tashkeel, 1, 50), CancellationToken.None);
        await sp.GetRequiredService<GetRootMentionedSurahsHandler>()
            .HandleAsync(new GetRootMentionedSurahsQuery(RootId), CancellationToken.None);
        await sp.GetRequiredService<GetRootMissingSurahsHandler>()
            .HandleAsync(new GetRootMissingSurahsQuery(RootId), CancellationToken.None);
        await sp.GetRequiredService<GetRootLemmasHandler>()
            .HandleAsync(new GetRootLemmasQuery(RootId), CancellationToken.None);
        await sp.GetRequiredService<GetRootStemsHandler>()
            .HandleAsync(new GetRootStemsQuery(RootId), CancellationToken.None);

        rootText.Should().NotBeNullOrWhiteSpace();
        foreach (var entry in fixture.LoggingProvider.Entries)
        {
            AssertNoText(entry, rootText);
            AssertNoText(entry, SearchText);
        }
    }

    public static TheoryData<GetRootsPageQuery, string, string[]> GetRootsPageWarningCases => new()
    {
        {
            new GetRootsPageQuery(SearchText, "bogus", 1, 50), "invalidSort",
            ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"]
        },
        {
            new GetRootsPageQuery(SearchText, null, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "sort", "pageNumber", "pageSize", "hasSearch"]
        },
    };

    public static TheoryData<GetRootSummaryQuery, LogLevel, string[]> GetRootSummaryWarningCases => new()
    {
        { new GetRootSummaryQuery(0), LogLevel.Warning, ["feature", "operation", "reason", "rootId"] },
        { new GetRootSummaryQuery(UnknownRootId), LogLevel.Warning, ["feature", "operation", "rootId"] },
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
