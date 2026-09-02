using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke.Data;

// The other half of the two-status model: SmokeRoutePipelineTests proves each route answers its
// DerivedStatus against an empty schema, and these prove the same routes answer their Seeded expectation
// once the canonical dump is restored. Eight routes answer 404 there and 200 here, while every phrase
// route remains unavailable because the canonical artifact deliberately excludes its derived data.
// Those independent outcomes are why a seeded expectation is a second status rather than a payload
// flag on the first.
[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeDataReadTests(SmokeDataFixture fixture)
{
    private const string PhraseTablePrefix = "quran_phrase_";
    private const string PhraseStateTable = "quran_phrase_index_state";

    public static TheoryData<string> SeededPaths()
    {
        var paths = new TheoryData<string>();
        foreach (var route in SmokeRouteCatalog.Routes.Where(route => route.Seeded is not null))
        {
            paths.Add(route.Path);
        }

        return paths;
    }

    [SmokeDumpTheory]
    [MemberData(nameof(SeededPaths))]
    public async Task SeededRoute_AnswersItsCanonicalExpectation_AfterRestore(string path)
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

    // Residual guard on the restore, not the primary one: a non-zero pg_restore exit already throws inside
    // SmokeDataFixture.InitializeAsync, so no test in this collection ever reaches a container whose
    // restore reported failure. What is left for this test is the archive that exits zero and is still
    // short. xUnit orders cases by a hash of their unique id, not by declaration, so this makes no claim
    // about running first — it reports a short table alongside the read failures, naming the shortfall.
    [SmokeDumpFact]
    public async Task RestoredDatabase_MatchesManifestAndKeepsPhraseDataExcluded()
    {
        var restored = await fixture.CountRowsAsync(fixture.Manifest.Tables.Keys);

        var shortfalls = fixture.Manifest.Tables
            .Where(table => restored[table.Key] != table.Value)
            .Select(table => $"{table.Key}: manifest {table.Value}, restored {restored[table.Key]}")
            .ToArray();

        shortfalls.Should().BeEmpty();

        fixture.Manifest.Tables.Keys.Should().NotContain(
            table => table.StartsWith(PhraseTablePrefix, StringComparison.Ordinal));

        var phraseRows = await fixture.CountRowsWithPrefixAsync(PhraseTablePrefix);
        phraseRows.Should().ContainKey(PhraseStateTable);
        phraseRows[PhraseStateTable].Should().Be(1);
        phraseRows
            .Where(table => table.Key != PhraseStateTable)
            .Should().OnlyContain(table => table.Value == 0);
    }

    [SmokeDumpFact]
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
        rootsPage.GetProperty("totalCount").GetInt32().Should().Be(fixture.Manifest.RowCount("quran_roots"));

        var rootWordsPage = await AssertSuccessfulEmptyPageAsync(rootWordsResponse, pageSize);
        rootWordsPage.GetProperty("totalCount").GetInt32().Should().BePositive();

        var uniqueWordsPage = await AssertSuccessfulEmptyPageAsync(uniqueWordsResponse, pageSize);
        uniqueWordsPage.GetProperty("totalCount").GetInt32().Should()
            .Be(fixture.Manifest.RowCount("quran_words_unique_tashkeel"));
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
                data.GetProperty("totalCount").GetInt32().Should().Be(fixture.Manifest.RowCount(manifestTable));
                data.GetProperty("items").GetArrayLength().Should().BePositive();
                break;

            case SmokeSeededPayload.NonEmptyPage:
                data.GetProperty("items").GetArrayLength().Should().BePositive();
                break;

            case SmokeSeededPayload.CountedCollection(var property, var manifestTable):
                data.GetProperty(property).GetArrayLength().Should().Be(fixture.Manifest.RowCount(manifestTable));
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
