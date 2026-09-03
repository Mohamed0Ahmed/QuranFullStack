using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke.Data;

// The other half of the two-status model: SmokeRoutePipelineTests proves each route answers its
// DerivedStatus against an empty schema, and these prove the retained canonical read routes against the
// verified persistent Test Database Capability.
[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeDataReadTests(SmokeDataFixture fixture)
{
    public static TheoryData<string> SeededPaths()
    {
        var paths = new TheoryData<string>();
        foreach (var route in SmokeRouteCatalog.Routes.Where(route => route.Seeded is not null))
        {
            paths.Add(route.Path);
        }

        return paths;
    }

    [Theory]
    [MemberData(nameof(SeededPaths))]
    public async Task CanonicalRoute_AnswersItsReviewedExpectation(string path)
    {
        var seeded = SmokeRouteCatalog.ByPath(path).Seeded!;

        using var client = fixture.CreateClient();
        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(seeded.Status);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);

        if (seeded.Payload is not null)
        {
            AssertPayload(envelope.GetProperty("data"), seeded.Payload);
        }
    }

    [Fact]
    public async Task PersistentDatabase_MatchesTheIndependentReaderOracle()
    {
        var actual = await fixture.CountRowsAsync(fixture.Oracle.RowCounts.Keys);

        var mismatches = fixture.Oracle.RowCounts
            .Where(table => actual[table.Key] != table.Value)
            .Select(table => $"{table.Key}: oracle {table.Value}, actual {actual[table.Key]}")
            .ToArray();

        mismatches.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtremePositivePages_PreserveSuccessfulEmptyLexicalReads()
    {
        const int pageSize = 100;

        using var client = fixture.CreateClient();
        using var discoveryResponse = await client.GetAsync("/api/words/roots?page=1&pageSize=1");

        discoveryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var discoveryEnvelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(discoveryResponse);
        var rootId = discoveryEnvelope
            .GetProperty("data")
            .GetProperty("items")
            .EnumerateArray()
            .Single()
            .GetProperty("id")
            .GetInt32();

        using var rootsResponse = await client.GetAsync(
            $"/api/words/roots?page={int.MaxValue}&pageSize={pageSize}");
        using var rootWordsResponse = await client.GetAsync(
            $"/api/words/roots/{rootId}/words/tashkeel?page={int.MaxValue}&pageSize={pageSize}");
        using var uniqueWordsResponse = await client.GetAsync(
            $"/api/words/unique/tashkeel?page={int.MaxValue}&pageSize={pageSize}");

        var rootsPage = await AssertSuccessfulEmptyPageAsync(rootsResponse, pageSize);
        rootsPage.GetProperty("totalCount").GetInt32().Should().Be(fixture.Oracle.RowCounts["quran_roots"]);

        var rootWordsPage = await AssertSuccessfulEmptyPageAsync(rootWordsResponse, pageSize);
        rootWordsPage.GetProperty("totalCount").GetInt32().Should().BePositive();

        var uniqueWordsPage = await AssertSuccessfulEmptyPageAsync(uniqueWordsResponse, pageSize);
        uniqueWordsPage.GetProperty("totalCount").GetInt32().Should()
            .Be(fixture.Oracle.RowCounts["quran_words_unique_tashkeel"]);
    }

    private static async Task<JsonElement> AssertSuccessfulEmptyPageAsync(
        HttpResponseMessage response,
        int pageSize)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var page = envelope.GetProperty("data");

        page.GetProperty("page").GetInt32().Should().Be(int.MaxValue);
        page.GetProperty("pageSize").GetInt32().Should().Be(pageSize);
        page.GetProperty("items").GetArrayLength().Should().Be(0);

        return page;
    }

    private void AssertPayload(JsonElement data, SmokeSeededPayload payload)
    {
        switch (payload)
        {
            case SmokeSeededPayload.PagedTable(var manifestTable):
                data.GetProperty("totalCount").GetInt32().Should().Be(fixture.Oracle.RowCounts[manifestTable]);
                data.GetProperty("items").GetArrayLength().Should().BePositive();
                break;

            case SmokeSeededPayload.NonEmptyPage:
                data.GetProperty("items").GetArrayLength().Should().BePositive();
                break;

            case SmokeSeededPayload.CountedCollection(var property, var manifestTable):
                data.GetProperty(property).GetArrayLength().Should().Be(fixture.Oracle.RowCounts[manifestTable]);
                break;

            case SmokeSeededPayload.NonEmptyCollection(var property):
                data.GetProperty(property).GetArrayLength().Should().BePositive();
                break;

            case SmokeSeededPayload.EchoedKey(var property, var nestedProperty, var expectedValue):
                data.GetProperty(property).GetProperty(nestedProperty).GetString().Should().Be(expectedValue);
                break;

            case SmokeSeededPayload.PositiveCount(var property):
                data.GetProperty(property).GetInt32().Should().BePositive();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(payload), payload, "Unhandled seeded payload shape.");
        }
    }
}
