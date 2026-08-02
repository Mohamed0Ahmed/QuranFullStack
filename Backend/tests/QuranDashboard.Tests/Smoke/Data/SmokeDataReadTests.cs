using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke.Data;

// The other half of the two-status model: SmokeRoutePipelineTests proves each route answers its
// DerivedStatus against an empty schema, and these prove the same routes answer their Seeded expectation
// once the canonical dump is restored. Seven of the twelve — the Mushaf page, ayah study and word
// analysis reads, and all four unique-word reads at id 1 — answer 404 there and 200 here, which is the
// whole reason a seeded expectation is a second status rather than a payload flag on the first.
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

    [SmokeDumpTheory]
    [MemberData(nameof(SeededPaths))]
    public async Task SeededRoute_AnswersItsSeededExpectation_WithRealData(string path)
    {
        var seeded = SmokeRouteCatalog.ByPath(path).Seeded!;

        using var client = fixture.CreateClient();
        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(seeded.Status);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);

        AssertPayload(envelope.GetProperty("data"), seeded.Payload);
    }

    // Residual guard on the restore, not the primary one: a non-zero pg_restore exit already throws inside
    // SmokeDataFixture.InitializeAsync, so no test in this collection ever reaches a container whose
    // restore reported failure. What is left for this test is the archive that exits zero and is still
    // short. xUnit orders cases by a hash of their unique id, not by declaration, so this makes no claim
    // about running first — it reports a short table alongside the read failures, naming the shortfall.
    [SmokeDumpFact]
    public async Task RestoredDatabase_CarriesEveryRowCountTheManifestRecords()
    {
        var restored = await fixture.CountRowsAsync(fixture.Manifest.Tables.Keys);

        var shortfalls = fixture.Manifest.Tables
            .Where(table => restored[table.Key] != table.Value)
            .Select(table => $"{table.Key}: manifest {table.Value}, restored {restored[table.Key]}")
            .ToArray();

        shortfalls.Should().BeEmpty();
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
