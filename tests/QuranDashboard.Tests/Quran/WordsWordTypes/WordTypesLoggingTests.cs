using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;
using QuranDashboard.Tests.TestSupport.Logging;

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
        AssertNoRawQueryText(entry);
    }

    [Fact]
    public async Task RowsHandler_LogsSafeStructuredFields_WithoutQuranTextOrPayloads()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, "occurrences", 1, 25),
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
        AssertNoRawQueryText(entry);
    }

    [Fact]
    public async Task RowsHandler_LogsInvalidFilterWarning_WithStructuredFields()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("bad", "PN", "genitive", null, null, "occurrences", 1, 25),
            CancellationToken.None);

        var entry = SingleEntryFor<GetWordTypeRowsHandler>(LogLevel.Warning);
        entry.FieldNames().Should().BeEquivalentTo(["feature", "operation", "reason", "type", "childCode", "hasCaseFilter", "hasTenseFilter", "hasVoiceFilter"]);
        entry.GetValue<string>("feature").Should().Be("WordTypes");
        entry.GetValue<string>("operation").Should().Be("GetWordTypeRows");
        entry.GetValue<string>("reason").Should().Be("invalidFilter");
        entry.GetValue<string>("type").Should().Be("bad");
        entry.GetValue<string?>("childCode").Should().Be("PN");
        entry.GetValue<bool>("hasCaseFilter").Should().BeTrue();
        entry.GetValue<bool>("hasTenseFilter").Should().BeFalse();
        entry.GetValue<bool>("hasVoiceFilter").Should().BeFalse();
        AssertNoRawQueryText(entry);
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
        AssertNoRawQueryText(entry);
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

    private static void AssertNoRawQueryText(RecordingLoggerProvider.LogEntry entry)
    {
        entry.Message.Should().NotContain("كَلِمَة");
        entry.Message.ToLowerInvariant().Should().NotContain("select");
        entry.Message.ToLowerInvariant().Should().NotContain("quran_word_morphology");
        entry.StructuredFieldsWithoutOriginalFormat().Select(pair => pair.Value?.ToString()).Should().NotContain("كَلِمَة");
        entry.StructuredFieldsWithoutOriginalFormat().Select(pair => pair.Value?.ToString()).Should().NotContain("select");
        entry.StructuredFieldsWithoutOriginalFormat().Select(pair => pair.Value?.ToString()).Should().NotContain("quran_word_morphology");
    }
}
