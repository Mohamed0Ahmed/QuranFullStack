using Microsoft.Extensions.Logging;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Tests.TestSupport.Logging;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesLoggingTests(WordTypesTestFixture fixture)
{
    [Fact]
    public async Task RowsHandler_LogsSafeStructuredFields_WithoutQuranTextOrPayloads()
    {
        await using var scope = fixture.CreateScope();
        fixture.LoggingProvider.Clear();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        _ = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, "occurrences", 1, 25),
            CancellationToken.None);

        var entry = fixture.LoggingProvider.Entries.Single(log => log.Level == LogLevel.Information);
        entry.FieldNames().Should().Contain(["feature", "operation", "type", "pageNumber", "pageSize", "sort", "totalCount", "itemCount"]);
        entry.Message.Should().NotContain("كَلِمَة");
        entry.Message.ToLowerInvariant().Should().NotContain("select");
        entry.Message.ToLowerInvariant().Should().NotContain("quran_word_morphology");
    }
}
