using System.Net;
using System.Text.Json;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;

namespace QuranDashboard.Tests.Quran.WordsWordTypes;

// Feature 026 US1/US2/US3: search-length bound (InvalidFilter), and the split page-size caps —
// list reads 1..1000, detail reads 1..100 — plus the aligned controller defaults (list 1000, detail 100).
[Collection(nameof(WordTypesCollection))]
public sealed class WordTypesSearchValidationAndCapsTests(WordTypesTestFixture fixture)
{
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 1000, true)]
    [InlineData(1, 1001, false)]
    [InlineData(0, 1000, false)]
    [InlineData(1, 0, false)]
    public void IsValidListPaging_AcceptsUpToOneThousand(int page, int pageSize, bool expected) =>
        WordTypesHandlerValidation.IsValidListPaging(page, pageSize).Should().Be(expected);

    [Theory]
    [InlineData(1, 100, true)]
    [InlineData(1, 101, false)]
    [InlineData(0, 100, false)]
    public void IsValidDetailPaging_KeepsHundredCap(int page, int pageSize, bool expected) =>
        WordTypesHandlerValidation.IsValidDetailPaging(page, pageSize).Should().Be(expected);

    [Theory]
    [InlineData(null, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void IsValidSearch_BoundsTheTrimmedLength(int? length, bool expected)
    {
        var search = length is null ? null : new string('ك', length.Value);
        WordTypesHandlerValidation.IsValidSearch(search).Should().Be(expected);
    }

    [Theory]
    [InlineData("  كلمة  ", "كلمة")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizeSearch_TrimsAndCollapsesEmptyToNull(string? raw, string? expected) =>
        WordTypesHandlerValidation.NormalizeSearch(raw).Should().Be(expected);

    // (T009e) An over-length search maps to InvalidFilter (400); at the cap it is accepted.
    [Theory]
    [InlineData(65, typeof(GetWordTypeRowsOutcome.InvalidFilter))]
    [InlineData(64, typeof(GetWordTypeRowsOutcome.Success))]
    public async Task RowsHandler_RejectsOverLengthSearch(int searchLength, Type expectedOutcome)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, new string('ك', searchLength), "occurrences", 1, 25),
            CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcome);
    }

    // (T019) List reads accept pageSize 1000 and reject 1001 with InvalidPaging.
    [Theory]
    [InlineData(1000, typeof(GetWordTypeRowsOutcome.Success))]
    [InlineData(1001, typeof(GetWordTypeRowsOutcome.InvalidPaging))]
    public async Task RowsHandler_ListCap_AcceptsThousandRejectsOverCap(int pageSize, Type expectedOutcome)
    {
        await using var scope = fixture.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWordTypeRowsHandler>();

        var outcome = await handler.HandleAsync(
            new GetWordTypeRowsQuery("noun", null, null, null, null, null, "occurrences", 1, pageSize),
            CancellationToken.None);

        outcome.GetType().Should().Be(expectedOutcome);
    }

    // (T019/T022) HTTP surface: list caps + defaults on /table and /words, detail cap + defaults on the
    // grouped-words member read.
    [Theory]
    [InlineData("table?type=noun&pageSize=1000", HttpStatusCode.OK, null)]
    [InlineData("table?type=noun&pageSize=1001", HttpStatusCode.BadRequest, null)]
    [InlineData("words?type=noun", HttpStatusCode.OK, 1000)]
    [InlineData("table/roots/190700/words?type=noun&pageSize=101", HttpStatusCode.BadRequest, null)]
    [InlineData("table/roots/190700/words?type=noun", HttpStatusCode.OK, 100)]
    public async Task HttpPipeline_HonoursSplitCapsAndDefaults(string path, HttpStatusCode expectedStatus, int? expectedDefaultPageSize)
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync($"/api/words/word-types/{path}", CancellationToken.None);

        response.StatusCode.Should().Be(expectedStatus);

        if (expectedDefaultPageSize is null)
        {
            return;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("data").GetProperty("pageSize").GetInt32()
            .Should().Be(expectedDefaultPageSize.Value);
    }
}
