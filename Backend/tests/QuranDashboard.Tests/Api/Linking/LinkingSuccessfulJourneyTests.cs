using System.Net.Http.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

[Collection(nameof(LinkingCollection))]
public sealed class LinkingSuccessfulJourneyTests(LinkingTestFixture fixture)
{
    [Fact]
    public async Task OwnerConfirmation_AcceptsThenReturnsDurableOutcomeAndFreshProjections()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("successful-owner");
        var prepared = await scenario.PrepareReadyPreflightAsync(doorId);

        var idempotencyKey = Guid.NewGuid();
        var confirmationRequest = new { preflightToken = prepared.Token, idempotencyKey };
        using var acceptedConfirmation = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            confirmationRequest);
        acceptedConfirmation.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var acceptedSubmission = await ApiEnvelope.ReadDataAsync(acceptedConfirmation);
        acceptedSubmission.GetProperty("resourceKind").GetString().Should().Be("job");
        var jobId = acceptedSubmission.GetProperty("job").GetProperty("jobId").GetGuid();

        var terminalJob = await scenario.PollConfirmationAsync(
            jobId,
            status => status == "succeeded");
        terminalJob.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);

        using var outcomeResponse = await client.GetAsync(
            $"/api/linking/confirmation-outcomes/{idempotencyKey}");
        outcomeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var durableOutcome = await ApiEnvelope.ReadDataAsync(outcomeResponse);
        durableOutcome.GetProperty("idempotencyKey").GetGuid().Should().Be(idempotencyKey);
        durableOutcome.GetProperty("result").GetProperty("doorId").GetInt32().Should().Be(doorId);

        using var existingConfirmation = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            confirmationRequest);
        existingConfirmation.StatusCode.Should().Be(HttpStatusCode.OK);
        var existingSubmission = await ApiEnvelope.ReadDataAsync(existingConfirmation);
        existingSubmission.GetProperty("resourceKind").GetString().Should().Be("durable_outcome");
        existingSubmission.GetProperty("durableOutcome").GetProperty("idempotencyKey")
            .GetGuid().Should().Be(idempotencyKey);

        using var snapshotResponse = await client.GetAsync(
            $"/api/abwab/doors/{doorId}/links/snapshot");
        snapshotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshot = await ApiEnvelope.ReadDataAsync(snapshotResponse);
        snapshot.GetProperty("doorId").GetInt32().Should().Be(doorId);
        snapshot.GetProperty("records").GetArrayLength().Should().Be(1);
        snapshot.GetProperty("ayahs").EnumerateArray()
            .Select(ayah => ayah.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");

        using var projectionResponse = await client.GetAsync("/api/mushaf/ayahs/1:1/doors");
        projectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projection = await ApiEnvelope.ReadDataAsync(projectionResponse);
        projection.GetProperty("verseKey").GetString().Should().Be("1:1");
        projection.GetProperty("doorIds").EnumerateArray()
            .Select(id => id.GetInt32()).Should().Equal(doorId);
    }

    [Fact]
    public async Task OwnerConfirmation_ImmediateRepeatReturnsExistingJobBeforeProcessing()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreatePausedConfirmationClient();
        var scenario = new LinkingTestScenario(fixture, client);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        var doorId = await scenario.CreateTargetDoorAsync("existing-confirmation-job");
        var prepared = await scenario.PrepareReadyPreflightAsync(doorId);
        var idempotencyKey = Guid.NewGuid();
        var confirmationRequest = new { preflightToken = prepared.Token, idempotencyKey };

        using var acceptedResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            confirmationRequest);
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(acceptedResponse);
        accepted.GetProperty("resourceKind").GetString().Should().Be("job");
        var jobId = accepted.GetProperty("job").GetProperty("jobId").GetGuid();

        using var existingResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            confirmationRequest);
        existingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var existing = await ApiEnvelope.ReadDataAsync(existingResponse);
        existing.GetProperty("resourceKind").GetString().Should().Be("job");
        existing.GetProperty("job").GetProperty("jobId").GetGuid().Should().Be(jobId);

        await fixture.ProcessNextConfirmationAsync();
        var terminalJob = await scenario.PollConfirmationAsync(
            jobId,
            status => status == "succeeded");
        terminalJob.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
