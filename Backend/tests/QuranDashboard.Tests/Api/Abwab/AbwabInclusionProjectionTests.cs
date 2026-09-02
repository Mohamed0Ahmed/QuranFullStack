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
    public async Task RecursiveReplacementAndDetach_PreserveContinuityDerivedStateVersionsAndPublicProjections()
    {
        await fixture.ResetAsync();
        using var owner = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, owner);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var a = await scenario.CreateTargetDoorAsync("recursive-replacement-a");
        var b = await scenario.CreateTargetDoorAsync("recursive-replacement-b");
        var c = await scenario.CreateTargetDoorAsync("recursive-replacement-c");
        await ConfirmTwoAyahSourceAsync(scenario, owner, a, [12]);
        using var publicClient = fixture.CreateClient();
        var ab = await AddInclusionAndReadIdAsync(owner, publicClient, b, a);
        var bc = await AddInclusionAndReadIdAsync(owner, publicClient, c, b);
        var before = await ReadDoorProjectionsAsync(publicClient, a, b, c);
        before.SelectMany(state => state.VerseKeys).Should().Equal("1:1", "1:1", "1:1");

        await ConfirmTwoAyahSourceAsync(scenario, owner, a, [11]);

        var replaced = await ReadDoorProjectionsAsync(publicClient, a, b, c);
        for (var index = 0; index < replaced.Length; index++)
        {
            replaced[index].Version.Should().BeGreaterThan(before[index].Version);
        }
        replaced.SelectMany(state => state.VerseKeys).Should().Equal("1:2", "1:2", "1:2");
        replaced[0].UnitIds.Single().Should().NotBe(before[0].UnitIds.Single());
        replaced[1].UnitIds.Should().Equal(before[1].UnitIds);
        replaced[2].UnitIds.Should().Equal(before[2].UnitIds);
        (await ReadSyncsAsync(ab, bc)).Should().BeEquivalentTo(
        [
            new InclusionSync(ab, replaced[0].UnitIds.Single(), replaced[1].UnitIds.Single(), "active"),
            new InclusionSync(bc, replaced[1].UnitIds.Single(), replaced[2].UnitIds.Single(), "active"),
        ]);
        (await ReadTopologySourceIdsAsync(publicClient, b)).Should().Equal(a);
        (await ReadTopologySourceIdsAsync(publicClient, c)).Should().Equal(b);
        (await ReadProjectionAsync(publicClient, "1:1")).Should().BeEmpty();
        (await ReadProjectionAsync(publicClient, "1:2")).Should().Equal(a, b, c);

        using var detach = new HttpRequestMessage(HttpMethod.Delete, $"/api/abwab/doors/{b}/inclusions/{ab}")
        {
            Content = JsonContent.Create(new { expectedTargetDoorVersion = replaced[1].Version }),
        };
        using var detachedResponse = await owner.SendAsync(detach);
        detachedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(detachedResponse)).GetProperty("removedSynchronizedRecordCount")
            .GetInt32().Should().Be(1);

        var detached = await ReadDoorProjectionsAsync(publicClient, a, b, c);
        detached[0].Version.Should().Be(replaced[0].Version);
        detached[1].Version.Should().BeGreaterThan(replaced[1].Version);
        detached[2].Version.Should().BeGreaterThan(replaced[2].Version);
        detached[0].VerseKeys.Should().Equal("1:2");
        detached[1].UnitIds.Should().BeEmpty();
        detached[2].UnitIds.Should().BeEmpty();
        (await ReadTopologySourceIdsAsync(publicClient, b)).Should().BeEmpty();
        (await ReadTopologySourceIdsAsync(publicClient, c)).Should().Equal(b);
        (await ReadProjectionAsync(publicClient, "1:2")).Should().Equal(a);
        var tree = (await ReadPublicTreeAsync(publicClient)).Tree;
        FindDoor(tree, a).GetProperty("inclusionConsumerCount").GetInt32().Should().Be(0);
        FindDoor(tree, b).GetProperty("inclusionSourceCount").GetInt32().Should().Be(0);
        FindDoor(tree, b).GetProperty("inclusionConsumerCount").GetInt32().Should().Be(1);
        FindDoor(tree, c).GetProperty("inclusionSourceCount").GetInt32().Should().Be(1);
        (await ReadContributionAsync(ab)).Should().Be(new ContributionState(0, true, 0));
        (await ReadContributionAsync(bc)).Should().Be(new ContributionState(0, false, 0));
        (await ReadSyncsAsync(ab, bc)).Should().BeEmpty();
    }

    [Fact]
    public async Task OverrideSuppressionAndLaterUpstreamChanges_CorrectSurvivingDirectContributionMetadata()
    {
        await fixture.ResetAsync();
        using var owner = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, owner);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var source = await scenario.CreateTargetDoorAsync("override-suppression-source");
        var target = await scenario.CreateTargetDoorAsync("override-suppression-target");
        await ConfirmTwoAyahSourceAsync(scenario, owner, source, []);
        using var publicClient = fixture.CreateClient();
        var inclusion = await AddInclusionAndReadIdAsync(owner, publicClient, target, source);
        var initial = await ReadDoorProjectionsAsync(publicClient, source, target);
        var sourceUnits = initial[0].UnitsByAyah;
        var targetUnits = initial[1].UnitsByAyah;
        var words = initial[1].SelectableWordIds.Take(2).ToArray();
        words.Should().HaveCount(2);

        var targetVersion = await ReplaceWordsAsync(owner, target, targetUnits[11], initial[1].Version, 11, words[0]);
        targetVersion = await DeleteUnitsAsync(owner, target, targetVersion, targetUnits[12]);
        var directStates = await ReadSyncsAsync(inclusion);
        directStates.Should().BeEquivalentTo(
        [
            new InclusionSync(inclusion, sourceUnits[11], targetUnits[11], "overridden"),
            new InclusionSync(inclusion, sourceUnits[12], null, "suppressed"),
        ]);
        (await ReadContributionAsync(inclusion)).Should().Be(new ContributionState(1, false, 1));

        var sourceVersion = await ReplaceWordsAsync(owner, source, sourceUnits[11], initial[0].Version, 11, words[1]);
        var afterUpstreamEdit = (await ReadDoorProjectionsAsync(publicClient, target))[0];
        afterUpstreamEdit.Version.Should().Be(targetVersion);
        afterUpstreamEdit.VerseKeys.Should().Equal("1:1");
        afterUpstreamEdit.SelectedWordIds.Should().Equal(words[0]);
        (await ReadSyncsAsync(inclusion)).Should().BeEquivalentTo(directStates);

        var beforeDeletion = await ReadDirectContributionAsync(source);
        beforeDeletion.State.Should().Be(new ContributionState(2, false, 2));
        await DeleteUnitsAsync(owner, source, sourceVersion, sourceUnits[12]);
        var afterDeletion = await ReadDirectContributionAsync(source);
        afterDeletion.State.ResolvedAyahCount.Should().Be(1);
        afterDeletion.State.Deleted.Should().BeFalse();
        afterDeletion.State.UnitCount.Should().Be(1);
        afterDeletion.ResolvedAtUtc.Should().BeAfter(beforeDeletion.ResolvedAtUtc);
        (await ReadSyncsAsync(inclusion)).Should().BeEquivalentTo(
            [new InclusionSync(inclusion, sourceUnits[11], targetUnits[11], "overridden")]);

        var final = await ReadDoorProjectionsAsync(publicClient, source, target);
        final.SelectMany(state => state.VerseKeys).Should().Equal("1:1", "1:1");
        final[0].SelectedWordIds.Should().Equal(words[1]);
        final[1].SelectedWordIds.Should().Equal(words[0]);
        (await ReadProjectionAsync(publicClient, "1:1")).Should().Equal(source, target);
        (await ReadProjectionAsync(publicClient, "1:2")).Should().BeEmpty();
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

    private static async Task ConfirmTwoAyahSourceAsync(
        LinkingTestScenario scenario,
        HttpClient owner,
        int doorId,
        IReadOnlyList<int> excludedAyahIds)
    {
        var descriptor = TwoAyahSourceDescriptor();
        using var sourceResponse = await owner.PostAsJsonAsync(
            "/api/linking/sources/resolve-page",
            new
            {
                descriptor,
                expectedLinkingDataRevision = (long?)null,
                expectedSourceViewIdentity = (string?)null,
                view = new
                {
                    segment = "all",
                    inclusionMode = (string?)null,
                    ayahOverrideIds = Array.Empty<int>(),
                    typeCodes = Array.Empty<string>(),
                },
                page = 1,
                pageSize = 100,
            });
        sourceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var source = await ApiEnvelope.ReadDataAsync(sourceResponse);
        source.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("verseKey").GetString())
            .Should().Equal("1:1", "1:2");

        using var preflightResponse = await owner.PostAsJsonAsync(
            "/api/linking/preflights",
            new
            {
                preparationKey = Guid.NewGuid(),
                doorId,
                expectedLinkingDataRevision = source.GetProperty("linkingDataRevision").GetInt64(),
                sources = new[]
                {
                    new
                    {
                        orderValue = 1,
                        workspaceSource = (object?)null,
                        inlineSource = new
                        {
                            descriptor,
                            configuration = new
                            {
                                inclusionMode = "all_except",
                                ayahOverrideIds = excludedAyahIds,
                                selectedWords = Array.Empty<object>(),
                                automaticWordMatchesEnabled = (bool?)null,
                                manualLinkShape = "independent",
                                descriptions = Array.Empty<object>(),
                            },
                        },
                    },
                },
            });
        preflightResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(preflightResponse);
        var preflightId = accepted.GetProperty("preflightId").GetGuid();
        var ready = await scenario.PollPreflightAsync(preflightId, status => status == "ready");
        ready.GetProperty("isBlocked").GetBoolean().Should().BeFalse();
        var token = ready.GetProperty("preflightToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();

        using var confirmationResponse = await owner.PostAsJsonAsync(
            $"/api/linking/preflights/{preflightId}/confirmation-jobs",
            new { preflightToken = token, idempotencyKey = Guid.NewGuid() });
        confirmationResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var confirmation = await ApiEnvelope.ReadDataAsync(confirmationResponse);
        await scenario.PollConfirmationAsync(
            confirmation.GetProperty("job").GetProperty("jobId").GetGuid(),
            status => status == "succeeded");
    }

    private static object TwoAyahSourceDescriptor() => new
    {
        kind = "manual-mushaf-ayahs",
        label = "آيتا الفاتحة الأولى والثانية",
        manualAyahs = new[]
        {
            new { verseKey = "1:1", pageNumber = 1, displayHint = "1:1" },
            new { verseKey = "1:2", pageNumber = 1, displayHint = "1:2" },
        },
        contextKey = (string?)null,
    };

    private static async Task<int> AddInclusionAndReadIdAsync(
        HttpClient owner,
        HttpClient publicClient,
        int targetDoorId,
        int sourceDoorId)
    {
        var tree = (await ReadPublicTreeAsync(publicClient)).Tree;
        var version = FindDoor(tree, targetDoorId).GetProperty("version").GetUInt32();
        using var response = await AddInclusionAsync(owner, targetDoorId, version, sourceDoorId);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("added")
            .EnumerateArray().Single().GetProperty("inclusionId").GetInt32();
    }

    private static async Task<DoorProjectionState[]> ReadDoorProjectionsAsync(
        HttpClient client,
        params int[] doorIds)
    {
        var states = new List<DoorProjectionState>();
        foreach (var doorId in doorIds)
        {
            using var response = await client.GetAsync($"/api/abwab/doors/{doorId}/links/snapshot");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var snapshot = await ApiEnvelope.ReadDataAsync(response);
            var ayahVerseKeys = snapshot.GetProperty("ayahs").EnumerateArray()
                .ToDictionary(ayah => ayah.GetProperty("ayahId").GetInt32(), ayah => ayah.GetProperty("verseKey").GetString()!);
            var records = snapshot.GetProperty("records").EnumerateArray().ToArray();
            states.Add(new DoorProjectionState(
                snapshot.GetProperty("doorVersion").GetUInt32(),
                records.Select(record => record.GetProperty("unitId").GetInt64()).ToArray(),
                records.SelectMany(record => record.GetProperty("ayahs").EnumerateArray())
                    .Select(ayah => ayahVerseKeys[ayah.GetProperty("ayahId").GetInt32()]).ToArray(),
                records.ToDictionary(
                    record => record.GetProperty("ayahs").EnumerateArray().Single().GetProperty("ayahId").GetInt32(),
                    record => record.GetProperty("unitId").GetInt64()),
                records.SelectMany(record => record.GetProperty("ayahs").EnumerateArray())
                    .SelectMany(ayah => ayah.GetProperty("selectedWordIds").EnumerateArray())
                    .Select(wordId => wordId.GetInt32()).ToArray(),
                snapshot.GetProperty("ayahs").EnumerateArray()
                    .SelectMany(ayah => ayah.GetProperty("words").EnumerateArray())
                    .Where(word => !word.GetProperty("isAyahMarker").GetBoolean())
                    .Select(word => word.GetProperty("quranWordId").GetInt32()).ToArray()));
        }

        return states.ToArray();
    }

    private static async Task<uint> ReplaceWordsAsync(
        HttpClient owner,
        int doorId,
        long unitId,
        uint doorVersion,
        int ayahId,
        int wordId)
    {
        using var response = await owner.PatchAsJsonAsync(
            $"/api/abwab/doors/{doorId}/links/{unitId}/words",
            new
            {
                expectedDoorVersion = doorVersion,
                selectedWords = new[] { new { ayahId, quranWordId = wordId } },
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("doorVersion").GetUInt32();
    }

    private static async Task<uint> DeleteUnitsAsync(
        HttpClient owner,
        int doorId,
        uint doorVersion,
        params long[] unitIds)
    {
        using var response = await owner.PostAsJsonAsync(
            $"/api/abwab/doors/{doorId}/links/bulk-delete",
            new { expectedDoorVersion = doorVersion, selectionMode = "only", unitIds });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("doorVersion").GetUInt32();
    }

    private static async Task<int[]> ReadTopologySourceIdsAsync(HttpClient client, int doorId)
    {
        using var response = await client.GetAsync($"/api/abwab/doors/{doorId}/inclusions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("sources").EnumerateArray()
            .Select(source => source.GetProperty("doorId").GetInt32()).ToArray();
    }

    private static async Task<int[]> ReadProjectionAsync(HttpClient client, string verseKey)
    {
        using var response = await client.GetAsync($"/api/mushaf/ayahs/{verseKey}/doors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await ApiEnvelope.ReadDataAsync(response)).GetProperty("doorIds").EnumerateArray()
            .Select(doorId => doorId.GetInt32()).ToArray();
    }

    private async Task<InclusionSync[]> ReadSyncsAsync(params int[] inclusionIds)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT door_inclusion_id, source_unit_id, target_unit_id, state
            FROM abwab_door_inclusion_unit_syncs
            WHERE door_inclusion_id = ANY(@inclusion_ids)
            ORDER BY door_inclusion_id, source_unit_id;
            """,
            connection);
        command.Parameters.AddWithValue("inclusion_ids", inclusionIds);
        await using var reader = await command.ExecuteReaderAsync();
        var syncs = new List<InclusionSync>();
        while (await reader.ReadAsync())
        {
            syncs.Add(new InclusionSync(
                reader.GetInt32(0),
                reader.GetInt64(1),
                reader.IsDBNull(2) ? null : reader.GetInt64(2),
                reader.GetString(3)));
        }

        return syncs.ToArray();
    }

    private async Task<ContributionState> ReadContributionAsync(int inclusionId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT contribution.resolved_ayah_count,
                   contribution.deleted_at IS NOT NULL,
                   COUNT(mapping.unit_id)::integer
            FROM linking_source_contributions contribution
            LEFT JOIN linking_source_contribution_units mapping
              ON mapping.source_contribution_id = contribution.id
            WHERE contribution.door_inclusion_id = @inclusion_id
            GROUP BY contribution.id;
            """,
            connection);
        command.Parameters.AddWithValue("inclusion_id", inclusionId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new ContributionState(reader.GetInt32(0), reader.GetBoolean(1), reader.GetInt32(2));
    }

    private async Task<DirectContributionState> ReadDirectContributionAsync(int doorId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT contribution.resolved_ayah_count,
                   contribution.deleted_at IS NOT NULL,
                   COUNT(mapping.unit_id)::integer,
                   contribution.resolved_at_utc
            FROM linking_source_contributions contribution
            LEFT JOIN linking_source_contribution_units mapping
              ON mapping.source_contribution_id = contribution.id
            WHERE contribution.door_id = @door_id
              AND contribution.door_inclusion_id IS NULL
            GROUP BY contribution.id;
            """,
            connection);
        command.Parameters.AddWithValue("door_id", doorId);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new DirectContributionState(
            new ContributionState(reader.GetInt32(0), reader.GetBoolean(1), reader.GetInt32(2)),
            reader.GetFieldValue<DateTimeOffset>(3));
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

    private sealed record DoorProjectionState(
        uint Version,
        IReadOnlyList<long> UnitIds,
        IReadOnlyList<string> VerseKeys,
        IReadOnlyDictionary<int, long> UnitsByAyah,
        IReadOnlyList<int> SelectedWordIds,
        IReadOnlyList<int> SelectableWordIds);

    private sealed record InclusionSync(
        int InclusionId,
        long SourceUnitId,
        long? TargetUnitId,
        string State);

    private sealed record ContributionState(int ResolvedAyahCount, bool Deleted, int UnitCount);

    private sealed record DirectContributionState(ContributionState State, DateTimeOffset ResolvedAtUtc);
}
