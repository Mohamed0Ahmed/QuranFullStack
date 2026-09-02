using System.Net.Http.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

[Collection(nameof(LinkingCollection))]
public sealed class LinkingRecoveryAndAtomicityTests(LinkingTestFixture fixture)
{
    [Fact]
    public async Task StaleLinkingDataRevision_IsRejectedBeforeAnyWorkflowOrLinkWrite()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("stale-linking-data");
        var source = await scenario.ResolveSourceAsync();
        var currentRevision = source.GetProperty("linkingDataRevision").GetInt64();

        using var response = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            scenario.CreateInlinePreflightRequest(Guid.NewGuid(), doorId, currentRevision + 1));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await ApiEnvelope.ReadDataAsync(response);
        error.GetProperty("code").GetString().Should().Be("LINKING_DATA_STALE");

        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(0, 0, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task StaleWorkspaceSourceRevision_IsRejectedBeforeAnyWorkflowOrLinkWrite()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("stale-workspace-source");
        var workspaceSource = await AddWorkspaceSourceAsync(client);
        var source = await scenario.ResolveSourceAsync();
        var currentRevision = source.GetProperty("linkingDataRevision").GetInt64();

        using var response = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            new
            {
                preparationKey = Guid.NewGuid(),
                doorId,
                expectedLinkingDataRevision = currentRevision,
                sources = new[]
                {
                    new
                    {
                        orderValue = 1,
                        workspaceSource = new
                        {
                            sourceId = workspaceSource.Id,
                            sourceVersion = workspaceSource.Version + 1,
                        },
                        inlineSource = (object?)null,
                    },
                },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await ApiEnvelope.ReadDataAsync(response);
        error.GetProperty("code").GetString().Should().Be("WORKSPACE_SOURCE_STALE");

        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(0, 0, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task RepeatedPreflightRequest_ReusesOneLifecycleAcrossActiveAndTerminalResponses()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("preflight-idempotency");
        var source = await scenario.ResolveSourceAsync();
        var currentRevision = source.GetProperty("linkingDataRevision").GetInt64();
        var preparationKey = Guid.NewGuid();
        var request = scenario.CreateInlinePreflightRequest(preparationKey, doorId, currentRevision);

        using var acceptedResponse = await client.PostAsJsonAsync("/api/linking/preflights", request);
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(acceptedResponse);
        var preflightId = accepted.GetProperty("preflightId").GetGuid();

        using var immediateRepeatResponse = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            request);
        var immediateRepeat = await ApiEnvelope.ReadDataAsync(immediateRepeatResponse);
        var immediateRepeatCode = immediateRepeat.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
        immediateRepeatResponse.StatusCode.Should().Be(
            HttpStatusCode.Accepted,
            "the exact preparation request should be idempotent, but lifecycle code was {0}",
            immediateRepeatCode);
        immediateRepeat.GetProperty("preflightId").GetGuid().Should().Be(preflightId);

        await fixture.ProcessNextPausedPreflightAsync();
        var ready = await scenario.PollPreflightAsync(preflightId, status => status == "ready");
        ready.GetProperty("preflightToken").GetString().Should().NotBeNullOrWhiteSpace();

        using var cancelResponse = await client.DeleteAsync($"/api/linking/preflights/{preflightId}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await ApiEnvelope.ReadDataAsync(cancelResponse);
        cancelled.GetProperty("status").GetString().Should().Be("cancelled");
        cancelled.GetProperty("failureCode").GetString().Should().Be("PREFLIGHT_CANCELLED");

        using var terminalRepeatResponse = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            request);
        terminalRepeatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var terminalRepeat = await ApiEnvelope.ReadDataAsync(terminalRepeatResponse);
        terminalRepeat.GetProperty("preflightId").GetGuid().Should().Be(preflightId);
        terminalRepeat.GetProperty("status").GetString().Should().Be("cancelled");

        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 0, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task PreparationKey_ReusedWithDifferentContent_IsRejectedWithoutExtraWrites()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("preflight-key-conflict");
        var source = await scenario.ResolveSourceAsync();
        var currentRevision = source.GetProperty("linkingDataRevision").GetInt64();
        var preparationKey = Guid.NewGuid();

        using var acceptedResponse = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            scenario.CreateInlinePreflightRequest(preparationKey, doorId, currentRevision));
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var conflictingRepeatResponse = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            scenario.CreateInlinePreflightRequest(
                preparationKey,
                doorId,
                currentRevision,
                manualLinkShape: "grouped"));
        conflictingRepeatResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var conflict = await ApiEnvelope.ReadDataAsync(conflictingRepeatResponse);
        conflict.GetProperty("code").GetString().Should().Be("IDEMPOTENCY_CONFLICT");

        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 0, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task RepeatedConfirmationRequest_ReusesQueuedAndCancelledJobResponses()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("confirmation-replay");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var idempotencyKey = Guid.NewGuid();
        var request = new { preflightToken = prepared.Token, idempotencyKey };

        using var acceptedResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            request);
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(acceptedResponse);
        var jobId = accepted.GetProperty("job").GetProperty("jobId").GetGuid();

        using var repeatedResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            request);
        repeatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeated = await ApiEnvelope.ReadDataAsync(repeatedResponse);
        repeated.GetProperty("resourceKind").GetString().Should().Be("job");
        repeated.GetProperty("job").GetProperty("jobId").GetGuid().Should().Be(jobId);

        using var cancelResponse = await client.DeleteAsync(
            $"/api/linking/confirmation-jobs/{jobId}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await ApiEnvelope.ReadDataAsync(cancelResponse);
        cancelled.GetProperty("status").GetString().Should().Be("cancelled");
        cancelled.GetProperty("failureCode").GetString().Should().Be("CONFIRMATION_CANCELLED");

        using var terminalRepeatResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            request);
        terminalRepeatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var terminalRepeat = await ApiEnvelope.ReadDataAsync(terminalRepeatResponse);
        terminalRepeat.GetProperty("resourceKind").GetString().Should().Be("job");
        terminalRepeat.GetProperty("job").GetProperty("jobId").GetGuid().Should().Be(jobId);
        terminalRepeat.GetProperty("job").GetProperty("status").GetString().Should().Be("cancelled");

        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 1, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task BoundedConfirmationPollingTimeout_ReportsSanitizedLastBusinessState()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("confirmation-cancel");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var request = new
        {
            preflightToken = prepared.Token,
            idempotencyKey = Guid.NewGuid(),
        };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        Func<Task> impossibleWait = async () => _ = await fixture.PollDataAsync(
            client,
            $"/api/linking/confirmation-jobs/{jobId}",
            "confirmation-job",
            jobId.ToString("D"),
            data => data.GetProperty("status").GetString() == "succeeded",
            TimeSpan.FromMilliseconds(150));
        var timeout = await impossibleWait.Should().ThrowAsync<TimeoutException>();
        timeout.Which.Message.Should().Contain($"resourceId={jobId:D}");
        timeout.Which.Message.Should().Contain("lastBusinessState=status:queued,stage:loading-prepared");
        timeout.Which.Message.Should().Contain("sanitizedSqlTail=");
        timeout.Which.Message.Should().NotContain(fixture.ConnectionString);
    }

    [Fact]
    public async Task QueuedConfirmationCancellation_IsIdempotentAndLeavesNoPartialWrites()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("confirmation-cancel-idempotent");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var request = new
        {
            preflightToken = prepared.Token,
            idempotencyKey = Guid.NewGuid(),
        };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        using var cancelResponse = await client.DeleteAsync(
            $"/api/linking/confirmation-jobs/{jobId}");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await ApiEnvelope.ReadDataAsync(cancelResponse);
        cancelled.GetProperty("status").GetString().Should().Be("cancelled");
        cancelled.GetProperty("failureCode").GetString().Should().Be("CONFIRMATION_CANCELLED");

        using var repeatedCancelResponse = await client.DeleteAsync(
            $"/api/linking/confirmation-jobs/{jobId}");
        repeatedCancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeatedCancel = await ApiEnvelope.ReadDataAsync(repeatedCancelResponse);
        repeatedCancel.GetProperty("jobId").GetGuid().Should().Be(jobId);
        repeatedCancel.GetProperty("status").GetString().Should().Be("cancelled");

        var terminal = await scenario.PollConfirmationAsync(jobId, status => status == "cancelled");
        terminal.GetProperty("failureCode").GetString().Should().Be("CONFIRMATION_CANCELLED");
        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 1, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task QueuedConfirmation_AfterHostRestart_CompletesOnceAndReturnsDurableOutcome()
    {
        await fixture.ResetAsync();
        var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("restart-recovery");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var idempotencyKey = Guid.NewGuid();
        var request = new { preflightToken = prepared.Token, idempotencyKey };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        client.Dispose();
        await fixture.RestartPausedWorkersAsync();
        using var restartedClient = fixture.CreatePausedWorkersClient();
        var restartedScenario = new LinkingTestScenario(fixture, restartedClient);
        restartedScenario.ConfigureOwner();

        await fixture.ProcessNextPausedConfirmationAsync();
        var terminal = await restartedScenario.PollConfirmationAsync(
            jobId,
            status => status == "succeeded");
        terminal.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);
        (await fixture.ReadConfirmationAttemptCountAsync(jobId)).Should().Be(1);

        await AssertDurableReplayAsync(
            restartedClient,
            prepared,
            request,
            idempotencyKey,
            jobId,
            doorId);
        await AssertSingleCommittedLinkAsync(restartedClient, doorId);
    }

    [Fact]
    public async Task ExpiredWorkerLease_IsRecoveredByOneNewAttemptWithoutDuplicateWrites()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("lease-recovery");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var idempotencyKey = Guid.NewGuid();
        var request = new { preflightToken = prepared.Token, idempotencyKey };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        await fixture.ClaimAndExpireConfirmationLeaseAsync(jobId);
        await fixture.ProcessNextPausedConfirmationAsync();

        var terminal = await scenario.PollConfirmationAsync(
            jobId,
            status => status == "succeeded");
        terminal.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);
        (await fixture.ReadConfirmationAttemptCountAsync(jobId)).Should().Be(2);
        await AssertDurableReplayAsync(
            client,
            prepared,
            request,
            idempotencyKey,
            jobId,
            doorId);
        await AssertSingleCommittedLinkAsync(client, doorId);
    }

    [Fact]
    public async Task InclusionSynchronizationConflict_RollsBackEverythingAndRemainsOneStaleJob()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("inclusion-source");
        var consumerDoorId = await scenario.CreateTargetDoorAsync("inclusion-consumer");
        var inclusionId = await AddDoorInclusionAsync(client, consumerDoorId, doorId);
        await fixture.RemoveInclusionSynchronizationContributionAsync(inclusionId);
        var prepared = await scenario.PrepareReadyPreflightAsync(
            doorId,
            fixture.ProcessNextPausedPreflightAsync);
        var idempotencyKey = Guid.NewGuid();
        var request = new { preflightToken = prepared.Token, idempotencyKey };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        await fixture.ProcessNextPausedConfirmationAsync();
        var terminal = await scenario.PollConfirmationAsync(
            jobId,
            status => status == "stale");
        terminal.GetProperty("failureCode").GetString().Should().Be("PREFLIGHT_STALE");
        terminal.GetProperty("result").ValueKind.Should().Be(JsonValueKind.Null);

        using var repeatResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            request);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeat = await ApiEnvelope.ReadDataAsync(repeatResponse);
        repeat.GetProperty("resourceKind").GetString().Should().Be("job");
        repeat.GetProperty("job").GetProperty("jobId").GetGuid().Should().Be(jobId);
        repeat.GetProperty("job").GetProperty("status").GetString().Should().Be("stale");

        using var outcomeResponse = await client.GetAsync(
            $"/api/linking/confirmation-outcomes/{idempotencyKey}");
        outcomeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 1, 0, 0, 0, 0));
        await AssertNoPublicLinksAsync(client, doorId);
    }

    [Fact]
    public async Task ConcurrentConfirmationAndInclusionAdd_SerializeWithoutDeadlockAndLeaveMatchingPublicProjections()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedWorkersClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var sourceDoorId = await scenario.CreateTargetDoorAsync("concurrent-confirmation-source");
        var targetDoorId = await scenario.CreateTargetDoorAsync("concurrent-confirmation-target");
        var prepared = await scenario.PrepareReadyPreflightAsync(
            sourceDoorId,
            fixture.ProcessNextPausedPreflightAsync);
        var idempotencyKey = Guid.NewGuid();
        var request = new { preflightToken = prepared.Token, idempotencyKey };
        var jobId = await EnqueueConfirmationAsync(client, prepared.Id, request);

        using var topologyResponse = await client.GetAsync($"/api/abwab/doors/{targetDoorId}/inclusions");
        topologyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var targetVersion = (await ApiEnvelope.ReadDataAsync(topologyResponse))
            .GetProperty("doorVersion").GetUInt32();

        await using var gateConnection = new NpgsqlConnection(fixture.ConnectionString);
        await gateConnection.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        await using (var gateCommand = new NpgsqlCommand(
                         "SELECT id FROM abwab_doors WHERE id = @door_id FOR UPDATE",
                         gateConnection,
                         gateTransaction))
        {
            gateCommand.Parameters.AddWithValue("door_id", sourceDoorId);
            (await gateCommand.ExecuteScalarAsync()).Should().Be(sourceDoorId);
        }
        await using var gatePidCommand = new NpgsqlCommand("SELECT pg_backend_pid()", gateConnection, gateTransaction);
        var gateBackendPid = Convert.ToInt32(await gatePidCommand.ExecuteScalarAsync());

        var confirmation = fixture.ProcessNextPausedConfirmationAsync();
        var inclusion = client.PostAsJsonAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions",
            new { expectedTargetDoorVersion = targetVersion, sourceDoorIds = new[] { sourceDoorId } });
        var observedWaiters = 0;
        try
        {
            observedWaiters = await WaitForBlockedSessionChainAsync(gateBackendPid, 2);
        }
        finally
        {
            await gateTransaction.CommitAsync();
        }

        await confirmation.WaitAsync(TimeSpan.FromSeconds(10));
        using var inclusionResponse = await inclusion.WaitAsync(TimeSpan.FromSeconds(10));
        observedWaiters.Should().BeGreaterThanOrEqualTo(2);
        inclusionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var added = await ApiEnvelope.ReadDataAsync(inclusionResponse);
        added.GetProperty("added").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("sourceDoorId").GetInt32().Should().Be(sourceDoorId);

        var terminal = await scenario.PollConfirmationAsync(jobId, status => status == "succeeded");
        terminal.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);
        using var outcomeResponse = await client.GetAsync(
            $"/api/linking/confirmation-outcomes/{idempotencyKey}");
        outcomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var outcome = await ApiEnvelope.ReadDataAsync(outcomeResponse);
        outcome.GetProperty("jobId").GetGuid().Should().Be(jobId);
        outcome.GetProperty("status").GetString().Should().Be("succeeded");

        await AssertDoorSnapshotAsync(client, sourceDoorId);
        await AssertDoorSnapshotAsync(client, targetDoorId);
        using var finalTopologyResponse = await client.GetAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions");
        finalTopologyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalTopology = await ApiEnvelope.ReadDataAsync(finalTopologyResponse);
        finalTopology.GetProperty("sources").EnumerateArray().Should().ContainSingle()
            .Which.GetProperty("doorId").GetInt32().Should().Be(sourceDoorId);

        using var treeResponse = await client.GetAsync("/api/abwab/tree");
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var doors = (await ApiEnvelope.ReadDataAsync(treeResponse)).GetProperty("doors")
            .EnumerateArray().ToDictionary(door => door.GetProperty("id").GetInt32());
        doors[sourceDoorId].GetProperty("inclusionConsumerCount").GetInt32().Should().Be(1);
        doors[targetDoorId].GetProperty("inclusionSourceCount").GetInt32().Should().Be(1);

        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("doorIds").EnumerateArray()
            .Select(id => id.GetInt32()).Should().Equal(sourceDoorId, targetDoorId);

        (await fixture.ReadPersistentStateCountsAsync(sourceDoorId)).Should().Be(
            new LinkingPersistentStateCounts(1, 1, 1, 1, 1, 1));
        (await fixture.ReadPersistentStateCountsAsync(targetDoorId)).Should().Be(
            new LinkingPersistentStateCounts(0, 0, 0, 1, 1, 1));
        await AssertSingleCompleteInclusionAsync(sourceDoorId, targetDoorId);
    }

    private static async Task<Guid> EnqueueConfirmationAsync(
        HttpClient client,
        Guid preflightId,
        object request)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{preflightId}/confirmation-jobs",
            request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(response);
        accepted.GetProperty("resourceKind").GetString().Should().Be("job");
        return accepted.GetProperty("job").GetProperty("jobId").GetGuid();
    }

    private static async Task AssertDurableReplayAsync(
        HttpClient client,
        PreparedLinking prepared,
        object request,
        Guid idempotencyKey,
        Guid jobId,
        int doorId)
    {
        using var outcomeResponse = await client.GetAsync(
            $"/api/linking/confirmation-outcomes/{idempotencyKey}");
        outcomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var outcome = await ApiEnvelope.ReadDataAsync(outcomeResponse);
        outcome.GetProperty("jobId").GetGuid().Should().Be(jobId);
        outcome.GetProperty("status").GetString().Should().Be("succeeded");
        outcome.GetProperty("result").GetProperty("doorId").GetInt32().Should().Be(doorId);

        using var repeatResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            request);
        repeatResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var repeat = await ApiEnvelope.ReadDataAsync(repeatResponse);
        repeat.GetProperty("resourceKind").GetString().Should().Be("durable_outcome");
        repeat.GetProperty("job").ValueKind.Should().Be(JsonValueKind.Null);
        repeat.GetProperty("durableOutcome").GetProperty("jobId").GetGuid().Should().Be(jobId);
    }

    private async Task AssertSingleCommittedLinkAsync(HttpClient client, int doorId)
    {
        var counts = await fixture.ReadPersistentStateCountsAsync(doorId);
        counts.Should().Be(new LinkingPersistentStateCounts(1, 1, 1, 1, 1, 1));

        using var snapshotResponse = await client.GetAsync($"/api/abwab/doors/{doorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await ApiEnvelope.ReadDataAsync(snapshotResponse);
        snapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        snapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");

        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("doorIds").EnumerateArray()
            .Select(id => id.GetInt32()).Should().Equal(doorId);
    }

    private static async Task AssertDoorSnapshotAsync(HttpClient client, int doorId)
    {
        using var response = await client.GetAsync($"/api/abwab/doors/{doorId}/links/snapshot");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await ApiEnvelope.ReadDataAsync(response);
        snapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        snapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");
    }

    private async Task<int> WaitForBlockedSessionChainAsync(int gateBackendPid, int minimumWaiters)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        var observed = 0;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            WITH RECURSIVE blocked(pid) AS (
                SELECT activity.pid
                FROM pg_stat_activity activity
                WHERE @gate_backend_pid = ANY (pg_blocking_pids(activity.pid))
                UNION
                SELECT activity.pid
                FROM pg_stat_activity activity
                JOIN blocked blocker ON blocker.pid = ANY (pg_blocking_pids(activity.pid))
            )
            SELECT COUNT(*) FROM blocked
            """,
            connection);
        command.Parameters.AddWithValue("gate_backend_pid", gateBackendPid);
        while (DateTimeOffset.UtcNow < deadline)
        {
            observed = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (observed >= minimumWaiters)
            {
                return observed;
            }

            await Task.Yield();
        }

        return observed;
    }

    private async Task AssertSingleCompleteInclusionAsync(int sourceDoorId, int targetDoorId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT COUNT(*) FROM abwab_door_inclusions
                 WHERE source_door_id = @source_door_id
                   AND target_door_id = @target_door_id
                   AND deleted_at IS NULL),
                (SELECT COUNT(*) FROM abwab_door_inclusion_unit_syncs sync
                 JOIN abwab_door_inclusions inclusion ON inclusion.id = sync.door_inclusion_id
                 WHERE inclusion.source_door_id = @source_door_id
                   AND inclusion.target_door_id = @target_door_id
                   AND inclusion.deleted_at IS NULL),
                (SELECT COUNT(*) FROM linking_source_contributions contribution
                 JOIN abwab_door_inclusions inclusion ON inclusion.id = contribution.door_inclusion_id
                 WHERE inclusion.source_door_id = @source_door_id
                   AND inclusion.target_door_id = @target_door_id
                   AND inclusion.deleted_at IS NULL
                   AND contribution.deleted_at IS NULL)
            """,
            connection);
        command.Parameters.AddWithValue("source_door_id", sourceDoorId);
        command.Parameters.AddWithValue("target_door_id", targetDoorId);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        reader.GetInt64(0).Should().Be(1);
        reader.GetInt64(1).Should().Be(1);
        reader.GetInt64(2).Should().Be(1);
    }

    private static async Task<WorkspaceSource> AddWorkspaceSourceAsync(HttpClient client)
    {
        using var loadResponse = await client.GetAsync("/api/linking/workspace");
        loadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspace = await ApiEnvelope.ReadDataAsync(loadResponse);
        workspace.GetProperty("sources").GetArrayLength().Should().Be(0);

        using var addResponse = await client.PostAsJsonAsync(
            "/api/linking/workspace/sources",
            new
            {
                descriptor = LinkingTestScenario.ManualSourceDescriptor(),
                initialConfiguration = new
                {
                    inclusionMode = "all_except",
                    ayahOverrides = Array.Empty<int>(),
                    selectedWords = Array.Empty<object>(),
                    automaticWordMatchesEnabled = (bool?)null,
                    manualLinkShape = "independent",
                    descriptions = Array.Empty<object>(),
                },
                workspaceVersion = (uint?)null,
            });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ApiEnvelope.ReadDataAsync(addResponse);
        var source = updated.GetProperty("sources").EnumerateArray().Should().ContainSingle().Subject;
        return new WorkspaceSource(
            source.GetProperty("id").GetInt64(),
            source.GetProperty("sourceVersion").GetUInt32());
    }

    private static async Task<int> AddDoorInclusionAsync(
        HttpClient client,
        int targetDoorId,
        int sourceDoorId)
    {
        using var topologyResponse = await client.GetAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions");
        topologyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var topology = await ApiEnvelope.ReadDataAsync(topologyResponse);
        var targetDoorVersion = topology.GetProperty("doorVersion").GetUInt32();

        using var addResponse = await client.PostAsJsonAsync(
            $"/api/abwab/doors/{targetDoorId}/inclusions",
            new
            {
                expectedTargetDoorVersion = targetDoorVersion,
                sourceDoorIds = new[] { sourceDoorId },
            });
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ApiEnvelope.ReadDataAsync(addResponse);
        return result.GetProperty("added").EnumerateArray().Single()
            .GetProperty("inclusionId").GetInt32();
    }

    private static async Task AssertNoPublicLinksAsync(HttpClient client, int doorId)
    {
        using var snapshotResponse = await client.GetAsync($"/api/abwab/doors/{doorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await ApiEnvelope.ReadDataAsync(snapshotResponse);
        snapshot.GetProperty("records").GetArrayLength().Should().Be(0);
        snapshot.GetProperty("ayahs").GetArrayLength().Should().Be(0);

        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("doorIds").GetArrayLength().Should().Be(0);
    }

    private sealed record WorkspaceSource(long Id, uint Version);
}
