using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;
using QuranDashboard.Infrastructure.Caching.Quran.Words;

namespace QuranDashboard.Tests.Quran.Words;

[Collection(nameof(UniqueWordsCollection))]
public sealed class UniqueWordsLoggingTests(UniqueWordsTestFixture fixture)
{
    [Fact]
    public async Task GetUniqueWordsPage_logs_success_information_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", "امنوا", null, 1, 50),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordsPageOutcome.Success>();

        var entry = SingleEntryFor<GetUniqueWordsPageHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "kind", "sort", "pageNumber", "pageSize", "totalCount", "itemCount", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordsPage");
        entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        entry.GetValue<string>("sort").Should().Be(UniqueWordSortKeys.MushafOrder);
        entry.GetValue<int>("pageNumber").Should().Be(1);
        entry.GetValue<int>("pageSize").Should().Be(50);
        entry.GetValue<int>("totalCount").Should().Be(1);
        entry.GetValue<int>("itemCount").Should().Be(1);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        AssertNoRawSearchText(entry, "امنوا");
    }

    [Fact]
    public async Task GetUniqueWordsPage_does_not_emit_cache_reader_information_logs()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);

        await handler.HandleAsync(
            new GetUniqueWordsPageQuery("tashkeel", null, null, 1, 50),
            CancellationToken.None);

        fixture.LoggingProvider.Entries
            .Where(entry => entry.CategoryName == typeof(CachedUniqueWordsReader).FullName
                && entry.Level == LogLevel.Information)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [MemberData(nameof(GetUniqueWordsPageWarningCases))]
    public async Task GetUniqueWordsPage_logs_one_warning_for_controlled_refusals(
        GetUniqueWordsPageQuery query,
        Type expectedOutcomeType,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordsPageHandler>();

        var outcome = await handler.HandleAsync(query, CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcomeType);

        var entry = SingleEntryFor<GetUniqueWordsPageHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordsPage");
        if (entry.FieldNames().Contains("reason"))
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }
        AssertNoRawSearchText(entry, "امنوا");

        if (entry.FieldNames().Contains("kind"))
        {
            entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        }

        if (entry.FieldNames().Contains("sort"))
        {
            entry.GetValue<string>("sort").Should().Be(UniqueWordSortKeys.MushafOrder);
        }

        if (entry.FieldNames().Contains("hasSearch"))
        {
            entry.GetValue<bool>("hasSearch").Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetUniqueWordSummary_logs_success_information_with_safe_counts()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSummaryQuery("tashkeel", 1002),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSummaryOutcome.Success>();

        var entry = SingleEntryFor<GetUniqueWordSummaryHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "kind", "uniqueWordId", "occurrencesCount", "ayahsCount", "surahsCount", "missingSurahsCount"]);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordSummary");
        entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        entry.GetValue<int>("uniqueWordId").Should().Be(1002);
        entry.GetValue<int>("occurrencesCount").Should().Be(5);
        entry.GetValue<int>("ayahsCount").Should().Be(5);
        entry.GetValue<int>("surahsCount").Should().Be(5);
        entry.GetValue<int>("missingSurahsCount").Should().Be(109);
    }

    [Theory]
    [MemberData(nameof(GetUniqueWordSummaryWarningCases))]
    public async Task GetUniqueWordSummary_logs_one_warning_for_controlled_failures(
        GetUniqueWordSummaryQuery query,
        Type expectedOutcomeType,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSummaryHandler>();

        var outcome = await handler.HandleAsync(query, CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcomeType);

        var entry = SingleEntryFor<GetUniqueWordSummaryHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordSummary");
        if (entry.FieldNames().Contains("reason"))
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }

        if (entry.FieldNames().Contains("kind"))
        {
            entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        }

        if (entry.FieldNames().Contains("uniqueWordId"))
        {
            entry.GetValue<int>("uniqueWordId").Should().Be(query.Id);
        }
    }

    [Fact]
    public async Task GetUniqueWordSurahs_logs_success_information_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordSurahsQuery("tashkeel", 1002),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordSurahsOutcome.Success>();

        var entry = SingleEntryFor<GetUniqueWordSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "kind", "uniqueWordId", "surahCount", "itemCount"]);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordSurahs");
        entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        entry.GetValue<int>("uniqueWordId").Should().Be(1002);
        entry.GetValue<int>("surahCount").Should().Be(5);
        entry.GetValue<int>("itemCount").Should().Be(5);
    }

    [Theory]
    [MemberData(nameof(GetUniqueWordSurahsWarningCases))]
    public async Task GetUniqueWordSurahs_logs_one_warning_for_controlled_failures(
        GetUniqueWordSurahsQuery query,
        Type expectedOutcomeType,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordSurahsHandler>();

        var outcome = await handler.HandleAsync(query, CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcomeType);

        var entry = SingleEntryFor<GetUniqueWordSurahsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordSurahs");
        if (entry.FieldNames().Contains("reason"))
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }

        if (entry.FieldNames().Contains("kind"))
        {
            entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        }

        if (entry.FieldNames().Contains("uniqueWordId"))
        {
            entry.GetValue<int>("uniqueWordId").Should().Be(query.Id);
        }
    }

    [Fact]
    public async Task GetUniqueWordMissingSurahs_logs_success_information_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordMissingSurahsQuery("tashkeel", 1002),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordMissingSurahsOutcome.Success>();

        var entry = SingleEntryFor<GetUniqueWordMissingSurahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "kind", "uniqueWordId", "missingSurahCount", "itemCount"]);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordMissingSurahs");
        entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        entry.GetValue<int>("uniqueWordId").Should().Be(1002);
        entry.GetValue<int>("missingSurahCount").Should().Be(109);
        entry.GetValue<int>("itemCount").Should().Be(109);
    }

    [Theory]
    [MemberData(nameof(GetUniqueWordMissingSurahsWarningCases))]
    public async Task GetUniqueWordMissingSurahs_logs_one_warning_for_controlled_failures(
        GetUniqueWordMissingSurahsQuery query,
        Type expectedOutcomeType,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordMissingSurahsHandler>();

        var outcome = await handler.HandleAsync(query, CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcomeType);

        var entry = SingleEntryFor<GetUniqueWordMissingSurahsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordMissingSurahs");
        if (entry.FieldNames().Contains("reason"))
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }

        if (entry.FieldNames().Contains("kind"))
        {
            entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        }

        if (entry.FieldNames().Contains("uniqueWordId"))
        {
            entry.GetValue<int>("uniqueWordId").Should().Be(query.Id);
        }
    }

    [Fact]
    public async Task GetUniqueWordAyahs_logs_success_information_with_safe_fields()
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(
            new GetUniqueWordAyahsQuery("tashkeel", 2003, 1, 20),
            CancellationToken.None);

        outcome.Should().BeOfType<GetUniqueWordAyahsOutcome.Success>();

        var entry = SingleEntryFor<GetUniqueWordAyahsHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "kind", "uniqueWordId", "pageNumber", "pageSize", "totalCount", "itemCount"]);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordAyahs");
        entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        entry.GetValue<int>("uniqueWordId").Should().Be(2003);
        entry.GetValue<int>("pageNumber").Should().Be(1);
        entry.GetValue<int>("pageSize").Should().Be(20);
        entry.GetValue<int>("totalCount").Should().Be(2);
        entry.GetValue<int>("itemCount").Should().Be(2);
    }

    [Theory]
    [MemberData(nameof(GetUniqueWordAyahsWarningCases))]
    public async Task GetUniqueWordAyahs_logs_one_warning_for_controlled_failures(
        GetUniqueWordAyahsQuery query,
        Type expectedOutcomeType,
        string expectedReason,
        string[] expectedFieldNames)
    {
        fixture.LoggingProvider.Clear();

        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetUniqueWordAyahsHandler>();

        var outcome = await handler.HandleAsync(query, CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcomeType);

        var entry = SingleEntryFor<GetUniqueWordAyahsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(expectedFieldNames);
        entry.GetValue<string>("feature").Should().Be("Words");
        entry.GetValue<string>("operation").Should().Be("GetUniqueWordAyahs");
        if (entry.FieldNames().Contains("reason"))
        {
            entry.GetValue<string>("reason").Should().Be(expectedReason);
        }

        if (entry.FieldNames().Contains("kind"))
        {
            entry.GetValue<string>("kind").Should().Be(UniqueWordKindKeys.Tashkeel);
        }

        if (entry.FieldNames().Contains("uniqueWordId"))
        {
            entry.GetValue<int>("uniqueWordId").Should().Be(query.Id);
        }
    }

    public static TheoryData<GetUniqueWordsPageQuery, Type, string, string[]> GetUniqueWordsPageWarningCases => new()
    {
        { new GetUniqueWordsPageQuery("not-a-kind", "امنوا", null, 1, 50), typeof(GetUniqueWordsPageOutcome.InvalidKind), "invalidKind", ["feature", "operation", "reason", "pageNumber", "pageSize", "hasSearch"] },
        { new GetUniqueWordsPageQuery("tashkeel", "امنوا", "bogus", 1, 50), typeof(GetUniqueWordsPageOutcome.InvalidSort), "invalidSort", ["feature", "operation", "reason", "kind", "pageNumber", "pageSize", "hasSearch"] },
        { new GetUniqueWordsPageQuery("tashkeel", "امنوا", null, 0, 50), typeof(GetUniqueWordsPageOutcome.InvalidPaging), "invalidPaging", ["feature", "operation", "reason", "kind", "sort", "pageNumber", "pageSize", "hasSearch"] },
    };

    public static TheoryData<GetUniqueWordSummaryQuery, Type, string, string[]> GetUniqueWordSummaryWarningCases => new()
    {
        { new GetUniqueWordSummaryQuery("not-a-kind", 1002), typeof(GetUniqueWordSummaryOutcome.InvalidKind), "invalidKind", ["feature", "operation", "reason", "uniqueWordId"] },
        { new GetUniqueWordSummaryQuery("tashkeel", 0), typeof(GetUniqueWordSummaryOutcome.InvalidId), "invalidId", ["feature", "operation", "reason", "kind", "uniqueWordId"] },
        { new GetUniqueWordSummaryQuery("tashkeel", 999999), typeof(GetUniqueWordSummaryOutcome.NotFound), "notFound", ["feature", "operation", "kind", "uniqueWordId"] },
    };

    public static TheoryData<GetUniqueWordSurahsQuery, Type, string, string[]> GetUniqueWordSurahsWarningCases => new()
    {
        { new GetUniqueWordSurahsQuery("not-a-kind", 1002), typeof(GetUniqueWordSurahsOutcome.InvalidKind), "invalidKind", ["feature", "operation", "reason", "uniqueWordId"] },
        { new GetUniqueWordSurahsQuery("tashkeel", 0), typeof(GetUniqueWordSurahsOutcome.InvalidId), "invalidId", ["feature", "operation", "reason", "kind", "uniqueWordId"] },
        { new GetUniqueWordSurahsQuery("tashkeel", 999999), typeof(GetUniqueWordSurahsOutcome.NotFound), "notFound", ["feature", "operation", "kind", "uniqueWordId"] },
    };

    public static TheoryData<GetUniqueWordMissingSurahsQuery, Type, string, string[]> GetUniqueWordMissingSurahsWarningCases => new()
    {
        { new GetUniqueWordMissingSurahsQuery("not-a-kind", 1002), typeof(GetUniqueWordMissingSurahsOutcome.InvalidKind), "invalidKind", ["feature", "operation", "reason", "uniqueWordId"] },
        { new GetUniqueWordMissingSurahsQuery("tashkeel", 0), typeof(GetUniqueWordMissingSurahsOutcome.InvalidId), "invalidId", ["feature", "operation", "reason", "kind", "uniqueWordId"] },
        { new GetUniqueWordMissingSurahsQuery("tashkeel", 999999), typeof(GetUniqueWordMissingSurahsOutcome.NotFound), "notFound", ["feature", "operation", "kind", "uniqueWordId"] },
    };

    public static TheoryData<GetUniqueWordAyahsQuery, Type, string, string[]> GetUniqueWordAyahsWarningCases => new()
    {
        { new GetUniqueWordAyahsQuery("not-a-kind", 2003, 1, 20), typeof(GetUniqueWordAyahsOutcome.InvalidKind), "invalidKind", ["feature", "operation", "reason", "uniqueWordId", "pageNumber", "pageSize"] },
        { new GetUniqueWordAyahsQuery("tashkeel", 0, 1, 20), typeof(GetUniqueWordAyahsOutcome.InvalidId), "invalidId", ["feature", "operation", "reason", "kind", "uniqueWordId", "pageNumber", "pageSize"] },
        { new GetUniqueWordAyahsQuery("tashkeel", 2003, 0, 20), typeof(GetUniqueWordAyahsOutcome.InvalidPaging), "invalidPaging", ["feature", "operation", "reason", "kind", "uniqueWordId", "pageNumber", "pageSize"] },
        { new GetUniqueWordAyahsQuery("tashkeel", 999999, 1, 20), typeof(GetUniqueWordAyahsOutcome.NotFound), "notFound", ["feature", "operation", "kind", "uniqueWordId", "pageNumber", "pageSize"] },
    };

    private RecordingLoggerProvider.LogEntry SingleEntryFor<THandler>(LogLevel expectedLevel)
    {
        var categoryName = typeof(THandler).FullName;
        var entry = fixture.LoggingProvider.Entries
            .Where(entry => entry.CategoryName == categoryName)
            .Should()
            .ContainSingle()
            .Subject;
        entry.Level.Should().Be(expectedLevel);
        return entry;
    }

    private static void AssertNoRawSearchText(RecordingLoggerProvider.LogEntry entry, string searchText)
    {
        entry.Message.Should().NotContain(searchText);
        entry.StructuredFieldsWithoutOriginalFormat().Select(pair => pair.Value?.ToString()).Should().NotContain(searchText);
        entry.FieldNames().Should().NotContain("search");
    }
}
