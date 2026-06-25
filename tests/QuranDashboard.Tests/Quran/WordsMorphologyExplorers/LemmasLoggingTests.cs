using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsMorphologyExplorers;

[Collection(nameof(MorphologyExplorersCollection))]
public sealed class LemmasLoggingTests(MorphologyExplorersTestFixture fixture)
{
    private const int LemmaId = 500;
    private const int UnknownLemmaId = 999_999;
    private const string SearchText = "كلمة";

    [Fact]
    public async Task GetLemmasPage_logs_success_with_safe_fields_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(new GetLemmasPageQuery(null, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Lemmas");
        entry.GetValue<string>("operation").Should().Be("GetLemmasPage");
        entry.GetValue<string>("sort").Should().Be(LemmaSortKeys.MushafOrder);
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
    }

    [Fact]
    public async Task GetLemmasPage_with_search_logs_hasSearch_without_raw_search_text()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(new GetLemmasPageQuery(SearchText, null, 1, 50), CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.FieldNames().Should().NotContain("search");
        AssertNoText(entry, SearchText);
    }

    [Theory]
    [MemberData(nameof(GetLemmasPageWarningCases))]
    public async Task GetLemmasPage_logs_one_warning_for_controlled_refusals(
        GetLemmasPageQuery query,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmasPageHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetLemmasPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        AssertNoText(entry, SearchText);
    }

    [Fact]
    public async Task GetLemmaSummary_logs_success_with_safe_counts_only()
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        var outcome = await handler.HandleAsync(new GetLemmaSummaryQuery(LemmaId), CancellationToken.None);
        var summary = outcome.Should().BeOfType<GetLemmaSummaryOutcome.Success>().Subject.Summary;

        var entry = SingleEntryFor<GetLemmaSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(
            ["feature", "operation", "lemmaId", "dominantTypeCode", "otherTypesCount", "occurrencesCount", "ayahsCount", "surahsCount", "stemsCount"]);
        entry.GetValue<int>("lemmaId").Should().Be(LemmaId);
        entry.GetValue<int>("occurrencesCount").Should().Be(summary.OccurrencesCount);
        entry.GetValue<int>("stemsCount").Should().Be(summary.StemsCount);
        AssertNoText(entry, summary.LemmaText);
    }

    [Theory]
    [MemberData(nameof(GetLemmaSummaryWarningCases))]
    public async Task GetLemmaSummary_logs_one_warning_for_controlled_failures(
        GetLemmaSummaryQuery query,
        LogLevel expectedLevel,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetLemmaSummaryHandler>();

        await handler.HandleAsync(query, CancellationToken.None);

        var entry = SingleEntryFor<GetLemmaSummaryHandler>(expectedLevel);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<int>("lemmaId").Should().Be(query.Id);
    }

    public static TheoryData<GetLemmasPageQuery, string, string[]> GetLemmasPageWarningCases => new()
    {
        {
            new GetLemmasPageQuery(SearchText, "bogus", 1, 50), "invalidSort",
            ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"]
        },
        {
            new GetLemmasPageQuery(SearchText, null, 0, 50), "invalidPaging",
            ["feature", "operation", "reason", "sort", "pageNumber", "pageSize", "hasSearch"]
        },
    };

    public static TheoryData<GetLemmaSummaryQuery, LogLevel, string[]> GetLemmaSummaryWarningCases => new()
    {
        { new GetLemmaSummaryQuery(0), LogLevel.Warning, ["feature", "operation", "reason", "lemmaId"] },
        { new GetLemmaSummaryQuery(UnknownLemmaId), LogLevel.Warning, ["feature", "operation", "lemmaId"] },
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
