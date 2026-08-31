using System.Net.Http.Json;
using System.Net.Http.Headers;
using QuranDashboard.Tests.Api.Linking;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Abwab;

[Collection(nameof(LinkingCollection))]
public sealed class AbwabInclusionProjectionTests(LinkingTestFixture fixture)
{
    [Fact]
    public async Task AddInclusion_PersistsPublicTreeDetailVersionAndMushafProjection()
    {
        await fixture.ResetAsync();
        using var ownerClient = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, ownerClient);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();

        var sourceDoorId = await scenario.CreateTargetDoorAsync("inclusion-projection-source");
        var prepared = await scenario.PrepareReadyPreflightAsync(sourceDoorId);
        var confirmationKey = Guid.NewGuid();
        using var confirmationResponse = await ownerClient.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            new { preflightToken = prepared.Token, idempotencyKey = confirmationKey });
        confirmationResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var confirmation = await ApiEnvelope.ReadDataAsync(confirmationResponse);
        var jobId = confirmation.GetProperty("job").GetProperty("jobId").GetGuid();
        await scenario.PollConfirmationAsync(jobId, status => status == "succeeded");

        var targetDoorId = await scenario.CreateTargetDoorAsync("inclusion-projection-target");
        using var publicClient = fixture.CreateClient();
        var before = await ReadPublicTreeAsync(publicClient);
        var targetBefore = FindDoor(before.Tree, targetDoorId);
        targetBefore.GetProperty("inclusionSourceCount").GetInt32().Should().Be(0);

        using var addResponse = await ownerClient.PostAsJsonAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions",
            new
            {
                expectedTargetDoorVersion = targetBefore.GetProperty("version").GetUInt32(),
                sourceDoorIds = new[] { sourceDoorId },
            });
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var added = await ApiEnvelope.ReadDataAsync(addResponse);
        added.GetProperty("targetDoorVersion").GetUInt32().Should().BeGreaterThan(
            targetBefore.GetProperty("version").GetUInt32());
        added.GetProperty("added").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("sourceDoorId").GetInt32().Should().Be(sourceDoorId);

        using var conditionalTreeRequest = new HttpRequestMessage(HttpMethod.Get, "/api/abwab/tree");
        conditionalTreeRequest.Headers.IfNoneMatch.Add(before.ETag);
        using var conditionalTreeResponse = await publicClient.SendAsync(conditionalTreeRequest);
        conditionalTreeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        conditionalTreeResponse.Headers.ETag.Should().NotBe(before.ETag);
        var afterTree = await ApiEnvelope.ReadDataAsync(conditionalTreeResponse);
        afterTree.GetProperty("version").GetDateTimeOffset().Should().NotBe(
            before.Tree.GetProperty("version").GetDateTimeOffset());
        var targetAfter = FindDoor(afterTree, targetDoorId);
        targetAfter.GetProperty("version").GetUInt32().Should().Be(
            added.GetProperty("targetDoorVersion").GetUInt32());
        targetAfter.GetProperty("inclusionSourceCount").GetInt32().Should().Be(1);
        FindDoor(afterTree, sourceDoorId).GetProperty("inclusionConsumerCount").GetInt32().Should().Be(1);

        using var topologyResponse = await publicClient.GetAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions");
        topologyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var topology = await ApiEnvelope.ReadDataAsync(topologyResponse);
        topology.GetProperty("doorVersion").GetUInt32().Should().Be(
            added.GetProperty("targetDoorVersion").GetUInt32());
        topology.GetProperty("sources").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("doorId").GetInt32().Should().Be(sourceDoorId);

        using var snapshotResponse = await publicClient.GetAsync(
            $"/api/abwab/doors/{targetDoorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await ApiEnvelope.ReadDataAsync(snapshotResponse);
        snapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        snapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");

        using var projectionResponse = await publicClient.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("doorIds").EnumerateArray()
            .Select(doorId => doorId.GetInt32())
            .Should().Equal(sourceDoorId, targetDoorId);
    }

    private static async Task<(EntityTagHeaderValue ETag, JsonElement Tree)> ReadPublicTreeAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/abwab/tree");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = response.Headers.ETag;
        etag.Should().NotBeNull();
        return (
            etag!,
            await ApiEnvelope.ReadDataAsync(response));
    }

    private static JsonElement FindDoor(JsonElement tree, int doorId) => tree.GetProperty("doors")
        .EnumerateArray()
        .Single(door => door.GetProperty("id").GetInt32() == doorId);
}
