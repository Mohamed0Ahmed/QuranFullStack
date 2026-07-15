using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesLoggingTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task TreeHandler_LogsSafeStructuredFields_WithoutQuranTextOrPayloads()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeTreeHandler>();

        _ = await handler.HandleAsync(new GetWordTypeTreeQuery(), CancellationToken.None);

        var entry = SingleEntryFor<GetWordTypeTreeHandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "itemCount"]);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().Be("GetWordTypeTree");
        entry.GetValue<int>("itemCount").Should().Be(4);
        AssertNoSensitivePayload(entry);
    }

    [Fact]
    public async Task RowsHandler_LogsSafeStructuredFields_WithoutQuranTextOrPayloads()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, null, "occurrences", 1, 25),
            CancellationToken.None);

        var entry = SingleEntryFor<GetWordTypeRowsHandler>(LogLevel.Information);
        entry.FieldNames().Should().Contain(["feature", "operation", "type", "pageNumber", "pageSize", "sort", "totalCount", "itemCount"]);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().Be("GetWordTypeRows");
        entry.GetValue<string>("type").Should().Be("noun");
        entry.GetValue<int>("pageNumber").Should().Be(1);
        entry.GetValue<int>("pageSize").Should().Be(25);
        entry.GetValue<string>("sort").Should().Be("occurrences");
        entry.GetValue<int>("totalCount").Should().BeGreaterThan(0);
        entry.GetValue<int>("itemCount").Should().BeGreaterThan(0);
        AssertNoSensitivePayload(entry);
    }

    // FR-006: an active search is recorded only as a hasSearch boolean; the term text never reaches the log.
    [Fact]
    public async Task RowsHandler_WithActiveSearch_LogsHasSearchBoolean_NeverTheTerm()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();
        const string searchTerm = "كَلِم";

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, searchTerm, "occurrences", 1, 25),
            CancellationToken.None);

        var entry = SingleEntryFor<GetWordTypeRowsHandler>(LogLevel.Information);
        entry.GetValue<bool>("hasSearch").Should().BeTrue();
        entry.StructuredFields()
            .Select(pair => pair.Value)
            .OfType<string>()
            .Append(entry.Message)
            .Should().OnlyContain(value => !value.Contains(searchTerm, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RowsHandler_LogsInvalidFilterWarning_WithStructuredFields()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("bad", "PN", "genitive", null, null, null, "occurrences", 1, 25),
            CancellationToken.None);

        var entry = SingleEntryFor<GetWordTypeRowsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "reason", "type", "childCode", "hasCaseFilter", "hasTenseFilter", "hasVoiceFilter", "hasSearch"]);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().Be("GetWordTypeRows");
        entry.GetValue<string>("reason").Should().Be("invalidFilter");
        entry.GetValue<string>("type").Should().Be("bad");
        entry.GetValue<string?>("childCode").Should().Be("PN");
        entry.GetValue<bool>("hasCaseFilter").Should().BeTrue();
        entry.GetValue<bool>("hasTenseFilter").Should().BeFalse();
        entry.GetValue<bool>("hasVoiceFilter").Should().BeFalse();
        // Search presence is logged only as a boolean — the term text is never recorded (FR-006).
        entry.GetValue<bool>("hasSearch").Should().BeFalse();
        AssertNoSensitivePayload(entry);
    }

    [Theory]
    [InlineData(typeof(GetWordTypeSummaryHandler), 0, "N", "invalidIdentity")]
    [InlineData(typeof(GetWordTypeAyahsHandler), 0, "PN", "invalidIdentity")]
    [InlineData(typeof(GetWordTypeSurahsHandler), 0, "PN", "invalidIdentity")]
    public async Task DetailHandlers_RejectInvalidIdentity_WithStructuredFields(
        Type handlerType,
        int tashkeelWordId,
        string contextCode,
        string expectedReason)
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();

        switch (handlerType.Name)
        {
            case nameof(GetWordTypeSummaryHandler):
                _ = await scope.ServiceProvider.GetRequiredService<GetWordTypeSummaryHandler>()
                    .HandleAsync(new GetWordTypeSummaryQuery(tashkeelWordId, contextCode, null, null, null), CancellationToken.None);
                break;
            case nameof(GetWordTypeAyahsHandler):
                _ = await scope.ServiceProvider.GetRequiredService<GetWordTypeAyahsHandler>()
                    .HandleAsync(new GetWordTypeAyahsQuery(tashkeelWordId, contextCode, null, null, null, 1, 25), CancellationToken.None);
                break;
            case nameof(GetWordTypeSurahsHandler):
                _ = await scope.ServiceProvider.GetRequiredService<GetWordTypeSurahsHandler>()
                    .HandleAsync(new GetWordTypeSurahsQuery(tashkeelWordId, contextCode, null, null, null), CancellationToken.None);
                break;
            default:
                throw new InvalidOperationException($"Unexpected handler type {handlerType.FullName}.");
        }

        var entry = SingleEntryFor(handlerType, LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "reason", "tashkeelWordId", "contextCode"]);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().BeOneOf("GetWordTypeSummary", "GetWordTypeAyahs", "GetWordTypeSurahs");
        entry.GetValue<string>("reason").Should().Be(expectedReason);
        entry.GetValue<int>("tashkeelWordId").Should().Be(tashkeelWordId);
        entry.GetValue<string>("contextCode").Should().Be(contextCode);
        AssertNoSensitivePayload(entry);
    }

    // Each grouped detail handler logs a completion entry with safe kind/ID/scope metadata only — never
    // display text, Quran text, SQL, or a payload.
    [Fact]
    public async Task GroupedDetailsHandlers_LogSafeStructuredFieldsWithoutTextPayloadOrSql()
    {
        await using var scope = fixture.CreateScope();
        var services = scope.ServiceProvider;

        await AssertGroupedInfoLog<GetWordTypeGroupedSummaryHandler>(
            services,
            "GetWordTypeGroupedSummary",
            ["feature", "operation", "kind", "dimensionId", "type", "childCode"],
            handler => handler.HandleAsync(
                new GetWordTypeGroupedSummaryQuery("roots", 190700, "noun", null, null, null, null), CancellationToken.None));

        await AssertGroupedInfoLog<GetWordTypeGroupedWordsHandler>(
            services,
            "GetWordTypeGroupedWords",
            ["feature", "operation", "kind", "dimensionId", "type", "childCode", "pageNumber", "pageSize", "totalCount", "itemCount"],
            handler => handler.HandleAsync(
                new GetWordTypeGroupedWordsQuery("roots", 190700, "noun", null, null, null, null, 1, 25), CancellationToken.None));

        await AssertGroupedInfoLog<GetWordTypeGroupedAyahsHandler>(
            services,
            "GetWordTypeGroupedAyahs",
            ["feature", "operation", "kind", "dimensionId", "type", "childCode", "pageNumber", "pageSize", "totalCount", "itemCount"],
            handler => handler.HandleAsync(
                new GetWordTypeGroupedAyahsQuery("roots", 190700, "noun", null, null, null, null, 1, 25), CancellationToken.None));

        await AssertGroupedInfoLog<GetWordTypeGroupedSurahsHandler>(
            services,
            "GetWordTypeGroupedSurahs",
            ["feature", "operation", "kind", "dimensionId", "type", "childCode", "mentionedCount", "missingCount"],
            handler => handler.HandleAsync(
                new GetWordTypeGroupedSurahsQuery("roots", 190700, "noun", null, null, null, null), CancellationToken.None));
    }

    private async Task AssertGroupedInfoLog<THandler>(
        IServiceProvider services,
        string expectedOperation,
        IReadOnlyCollection<string> expectedFields,
        Func<THandler, Task> invoke)
        where THandler : class
    {
        fixture.LoggingProvider.Clear();
        var handler = services.GetRequiredService<THandler>();

        await invoke(handler);

        var entry = SingleEntryFor<THandler>(LogLevel.Information);
        entry.FieldNames().Should().BeEquivalentTo(expectedFields);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().Be(expectedOperation);
        entry.GetValue<string>("kind").Should().Be("roots");
        entry.GetValue<int>("dimensionId").Should().Be(190700);
        entry.GetValue<string>("type").Should().Be("noun");
        AssertNoSensitivePayload(entry);
    }

    private RecordingLoggerProvider.LogEntry SingleEntryFor<THandler>(LogLevel expectedLevel)
        where THandler : class
    {
        return SingleEntryFor(typeof(THandler), expectedLevel);
    }

    private RecordingLoggerProvider.LogEntry SingleEntryFor(Type handlerType, LogLevel expectedLevel)
    {
        var categoryName = handlerType.FullName;
        var entry = fixture.LoggingProvider.Entries
            .Where(entry => entry.CategoryName == categoryName)
            .Should()
            .ContainSingle()
            .Subject;
        entry.Level.Should().Be(expectedLevel);
        return entry;
    }

    private static void AssertNoSensitivePayload(RecordingLoggerProvider.LogEntry entry)
    {
        string[] forbiddenFragments =
        [
            "select",
            "from",
            "where",
            "join",
            "quran_",
            "ك ل م",
            "كَلِمَة",
            "بِسْمِ",
            "ٱللَّهِ",
        ];
        var loggedStrings = entry.StructuredFields()
            .Select(pair => pair.Value)
            .OfType<string>()
            .Append(entry.Message);

        foreach (var loggedString in loggedStrings)
        {
            foreach (var forbiddenFragment in forbiddenFragments)
            {
                loggedString.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"logs must not contain '{forbiddenFragment}'");
            }
        }
    }
}
