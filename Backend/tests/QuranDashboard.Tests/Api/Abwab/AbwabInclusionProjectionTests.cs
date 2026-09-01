using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Api.Linking;
using QuranDashboard.Tests.Smoke;
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

    [Fact]
    public async Task ValidInclusion_AnonymousUnderprivilegedRevokedAndDisabledActorsRemainDeniedWithoutPublicStateDrift()
    {
        await fixture.ResetAsync();
        using var ownerClient = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, ownerClient);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var sourceDoorId = await CreateLinkedSourceDoorAsync(scenario, ownerClient);
        var grantedTargetDoorId = await scenario.CreateTargetDoorAsync("inclusion-granted-target");
        var targetDoorId = await scenario.CreateTargetDoorAsync("inclusion-denial-target");
        using var publicClient = fixture.CreateClient();
        var initial = await ReadPublicStateAsync(publicClient, targetDoorId);
        var targetVersion = FindDoor(initial.Tree, targetDoorId).GetProperty("version").GetUInt32();

        using (var anonymousClient = fixture.CreateClient())
        using (var response = await AddInclusionAsync(anonymousClient, targetDoorId, targetVersion, sourceDoorId))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
        }

        const string actorSub = "abwab-inclusion-lifecycle-actor";
        var actor = await fixture.CreateActiveNonOwnerAsync(actorSub);
        using var actorClient = CreateAuthenticatedClient(actorSub);
        using (var response = await AddInclusionAsync(actorClient, targetDoorId, targetVersion, sourceDoorId))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                response, HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied);
        }
        AssertPublicStateUnchanged(initial, await ReadPublicStateAsync(publicClient, targetDoorId));

        var granted = await ReplacePermissionsAsync(
            ownerClient, actor.UserId, actor.Version, [AbwabPermissions.Inclusions.Create], "Grant inclusion creation before revocation.");
        var grantedTargetVersion = FindDoor(
            (await ReadPublicStateAsync(publicClient, grantedTargetDoorId)).Tree,
            grantedTargetDoorId).GetProperty("version").GetUInt32();
        using (var response = await AddInclusionAsync(actorClient, grantedTargetDoorId, grantedTargetVersion, sourceDoorId))
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var beforeDeniedWrites = await ReadPublicStateAsync(publicClient, targetDoorId);
        var revoked = await ReplacePermissionsAsync(
            ownerClient, actor.UserId, granted, [], "Revoke inclusion creation before the protected write.");
        using (var response = await AddInclusionAsync(actorClient, targetDoorId, targetVersion, sourceDoorId))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                response, HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied);
        }

        var regranted = await ReplacePermissionsAsync(
            ownerClient, actor.UserId, revoked, [AbwabPermissions.Inclusions.Create], "Restore the grant before disabling the actor.");
        using (var disableResponse = await ownerClient.PostAsJsonAsync(
                   $"/api/access/users/{actor.UserId}/disable",
                   new { expectedVersion = regranted, reason = "Disable the actor before the protected write." }))
        {
            disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var response = await AddInclusionAsync(actorClient, targetDoorId, targetVersion, sourceDoorId))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Forbidden, ApiMessages.AccessInactive);
        }

        AssertPublicStateUnchanged(beforeDeniedWrites, await ReadPublicStateAsync(publicClient, targetDoorId));
    }

    [Fact]
    public async Task AddInclusion_WithStaleTargetVersion_ReturnsConflictWithoutTopologyOrProjectionDrift()
    {
        await fixture.ResetAsync();
        using var ownerClient = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, ownerClient);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var sourceDoorId = await CreateLinkedSourceDoorAsync(scenario, ownerClient);
        var targetDoorId = await scenario.CreateTargetDoorAsync("inclusion-stale-target");
        using var publicClient = fixture.CreateClient();
        var original = await ReadPublicStateAsync(publicClient, targetDoorId);
        var staleVersion = FindDoor(original.Tree, targetDoorId).GetProperty("version").GetUInt32();

        using (var editResponse = await ownerClient.PutAsJsonAsync(
                   $"/api/abwab/doors/{targetDoorId}",
                   new
                   {
                       name = "باب حماية الربط inclusion-stale-target بعد التعديل",
                       description = (string?)null,
                       representativeAyahText = (string?)null,
                       aliases = Array.Empty<string>(),
                       version = staleVersion,
                   }))
        {
            editResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var beforeStaleRequest = await ReadPublicStateAsync(publicClient, targetDoorId);
        using (var staleResponse = await AddInclusionAsync(ownerClient, targetDoorId, staleVersion, sourceDoorId))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                staleResponse, HttpStatusCode.Conflict, ApiMessages.AbwabDoorInclusionsStaleTarget);
        }

        AssertPublicStateUnchanged(beforeStaleRequest, await ReadPublicStateAsync(publicClient, targetDoorId));
    }

    [Fact]
    public async Task ConcurrentAddInclusion_OneCommitAndOneConflictLeaveOneConsistentPublicProjection()
    {
        await fixture.ResetAsync();
        using var ownerClient = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, ownerClient);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var sourceDoorId = await CreateLinkedSourceDoorAsync(scenario, ownerClient);
        var targetDoorId = await scenario.CreateTargetDoorAsync("inclusion-concurrent-target");
        using var publicClient = fixture.CreateClient();
        var before = await ReadPublicStateAsync(publicClient, targetDoorId);
        var targetVersion = FindDoor(before.Tree, targetDoorId).GetProperty("version").GetUInt32();
        await using var gateConnection = new NpgsqlConnection(fixture.ConnectionString);
        await gateConnection.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        await using (var gateCommand = new NpgsqlCommand(
                         "SELECT id FROM abwab_doors WHERE id = @door_id FOR UPDATE;",
                         gateConnection,
                         gateTransaction))
        {
            gateCommand.Parameters.AddWithValue("door_id", targetDoorId);
            await gateCommand.ExecuteScalarAsync();
        }

        var first = AddInclusionAsync(ownerClient, targetDoorId, targetVersion, sourceDoorId);
        var second = AddInclusionAsync(ownerClient, targetDoorId, targetVersion, sourceDoorId);
        var observedWaiters = 0;
        try
        {
            observedWaiters = await WaitForDoorLockWaitersAsync(2);
        }
        finally
        {
            await gateTransaction.CommitAsync();
        }

        var responses = await Task.WhenAll(first, second);
        try
        {
            observedWaiters.Should().BeGreaterThanOrEqualTo(2);
            responses.Select(response => response.StatusCode).Should().BeEquivalentTo(
                [HttpStatusCode.Created, HttpStatusCode.Conflict]);
            var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(conflict);
            envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
            envelope.GetProperty("message").GetString().Should().BeOneOf(
                ApiMessages.AbwabDoorInclusionsStaleTarget,
                ApiMessages.AbwabDoorInclusionsDuplicate);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        var after = await ReadPublicStateAsync(publicClient, targetDoorId);
        FindDoor(after.Tree, targetDoorId).GetProperty("inclusionSourceCount").GetInt32().Should().Be(1);
        after.Topology.GetProperty("sources").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("doorId").GetInt32().Should().Be(sourceDoorId);
        after.Snapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        after.Projection.GetProperty("doorIds").EnumerateArray().Select(id => id.GetInt32())
            .Should().Equal(sourceDoorId, targetDoorId);
    }

    [Fact]
    public async Task RestoreDoor_WithStaleVersion_ReturnsConflictWithoutArchivedReadOrProjectionDrift()
    {
        await fixture.ResetAsync();
        using var ownerClient = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, ownerClient);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await CreateLinkedSourceDoorAsync(scenario, ownerClient);
        using var publicClient = fixture.CreateClient();
        var versionBeforeArchive = FindDoor((await ReadPublicTreeAsync(publicClient)).Tree, doorId)
            .GetProperty("version").GetUInt32();

        using (var archiveRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/abwab/doors/{doorId}")
               {
                   Content = JsonContent.Create(new { version = versionBeforeArchive }),
               })
        using (var archiveResponse = await ownerClient.SendAsync(archiveRequest))
        {
            archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var archivedBeforeStaleRestore = await ReadArchivedDoorStateAsync(publicClient, doorId);
        using (var restoreResponse = await ownerClient.PostAsJsonAsync(
                   $"/api/abwab/doors/{doorId}/restore",
                   new { sectionId = (int?)null, version = versionBeforeArchive }))
        {
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                restoreResponse, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
        }

        AssertArchivedDoorStateUnchanged(
            archivedBeforeStaleRestore,
            await ReadArchivedDoorStateAsync(publicClient, doorId));
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

    private async Task<int> CreateLinkedSourceDoorAsync(LinkingTestScenario scenario, HttpClient ownerClient)
    {
        var sourceDoorId = await scenario.CreateTargetDoorAsync($"inclusion-source-{Guid.NewGuid():N}");
        var prepared = await scenario.PrepareReadyPreflightAsync(sourceDoorId);
        using var confirmationResponse = await ownerClient.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            new { preflightToken = prepared.Token, idempotencyKey = Guid.NewGuid() });
        confirmationResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var confirmation = await ApiEnvelope.ReadDataAsync(confirmationResponse);
        await scenario.PollConfirmationAsync(
            confirmation.GetProperty("job").GetProperty("jobId").GetGuid(),
            status => status == "succeeded");
        return sourceDoorId;
    }

    private HttpClient CreateAuthenticatedClient(string sub)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));
        return client;
    }

    private static Task<HttpResponseMessage> AddInclusionAsync(
        HttpClient client, int targetDoorId, uint targetVersion, int sourceDoorId) =>
        client.PostAsJsonAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions",
            new { expectedTargetDoorVersion = targetVersion, sourceDoorIds = new[] { sourceDoorId } });

    private static async Task<uint> ReplacePermissionsAsync(
        HttpClient ownerClient,
        int userId,
        uint expectedVersion,
        IReadOnlyList<string> permissionCodes,
        string reason)
    {
        using var response = await ownerClient.PutAsJsonAsync(
            $"/api/access/users/{userId}/permissions",
            new { expectedVersion, permissionCodes, reason });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("version").GetUInt32();
    }

    private async Task<PublicInclusionState> ReadPublicStateAsync(HttpClient client, int targetDoorId)
    {
        var tree = await ReadPublicTreeAsync(client);
        using var topologyResponse = await client.GetAsync($"/api/abwab/doors/{targetDoorId}/inclusions");
        topologyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var snapshotResponse = await client.GetAsync($"/api/abwab/doors/{targetDoorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return new PublicInclusionState(
            tree.ETag.ToString(),
            tree.Tree,
            await ApiEnvelope.ReadDataAsync(topologyResponse),
            await ApiEnvelope.ReadDataAsync(snapshotResponse),
            await ApiEnvelope.ReadDataAsync(projectionResponse));
    }

    private async Task<int> WaitForDoorLockWaitersAsync(int minimumWaiters)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        var observed = 0;
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock';",
                connection);
            observed = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (observed >= minimumWaiters)
            {
                return observed;
            }

            await Task.Delay(25);
        }

        return observed;
    }

    private async Task<ArchivedDoorState> ReadArchivedDoorStateAsync(HttpClient client, int doorId)
    {
        var tree = await ReadPublicTreeAsync(client);
        using var snapshotResponse = await client.GetAsync($"/api/abwab/doors/{doorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return new ArchivedDoorState(
            tree.ETag.ToString(),
            tree.Tree,
            await snapshotResponse.Content.ReadAsStringAsync(),
            await ApiEnvelope.ReadDataAsync(projectionResponse));
    }

    private static void AssertPublicStateUnchanged(PublicInclusionState before, PublicInclusionState after)
    {
        after.TreeETag.Should().Be(before.TreeETag);
        after.Tree.GetRawText().Should().Be(before.Tree.GetRawText());
        after.Topology.GetRawText().Should().Be(before.Topology.GetRawText());
        after.Snapshot.GetRawText().Should().Be(before.Snapshot.GetRawText());
        after.Projection.GetRawText().Should().Be(before.Projection.GetRawText());
    }

    private static void AssertArchivedDoorStateUnchanged(ArchivedDoorState before, ArchivedDoorState after)
    {
        after.TreeETag.Should().Be(before.TreeETag);
        after.Tree.GetRawText().Should().Be(before.Tree.GetRawText());
        after.Snapshot.Should().Be(before.Snapshot);
        after.Projection.GetRawText().Should().Be(before.Projection.GetRawText());
    }

    private sealed record PublicInclusionState(
        string TreeETag,
        JsonElement Tree,
        JsonElement Topology,
        JsonElement Snapshot,
        JsonElement Projection);

    private sealed record ArchivedDoorState(
        string TreeETag,
        JsonElement Tree,
        string Snapshot,
        JsonElement Projection);
}
