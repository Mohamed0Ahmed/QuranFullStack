using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

[Collection(nameof(LinkingCollection))]
public sealed class LinkingSuccessfulJourneyTests(LinkingTestFixture fixture)
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task OwnerConfirmation_AcceptsThenReturnsDurableOutcomeAndFreshProjections()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClient();
        ConfigureOwner(client);
        await ProvisionOwnerAsync(client);
        var doorId = await CreateTargetDoorAsync(client);
        var prepared = await PrepareReadyPreflightAsync(client, doorId);

        var idempotencyKey = Guid.NewGuid();
        var confirmationRequest = new { preflightToken = prepared.Token, idempotencyKey };
        using var acceptedConfirmation = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            confirmationRequest);
        acceptedConfirmation.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var acceptedSubmission = await ApiEnvelope.ReadDataAsync(acceptedConfirmation);
        acceptedSubmission.GetProperty("resourceKind").GetString().Should().Be("job");
        var jobId = acceptedSubmission.GetProperty("job").GetProperty("jobId").GetGuid();

        var terminalJob = await PollDataAsync(
            client,
            $"/api/linking/confirmation-jobs/{jobId}",
            data => data.GetProperty("status").GetString() == "succeeded");
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
        ConfigureOwner(client);
        await ProvisionOwnerAsync(client);
        var doorId = await CreateTargetDoorAsync(client);
        var prepared = await PrepareReadyPreflightAsync(client, doorId);
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
        var terminalJob = await PollDataAsync(
            client,
            $"/api/linking/confirmation-jobs/{jobId}",
            data => data.GetProperty("status").GetString() == "succeeded");
        terminalJob.GetProperty("failureCode").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private async Task<PreparedLinking> PrepareReadyPreflightAsync(
        HttpClient client,
        int doorId)
    {
        var descriptor = ManualSourceDescriptor();
        var sourcePage = await ResolveSourceAsync(client, descriptor);
        var linkingDataRevision = sourcePage.GetProperty("linkingDataRevision").GetInt64();
        sourcePage.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");

        using var createPreflight = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            new
            {
                preparationKey = Guid.NewGuid(),
                doorId,
                expectedLinkingDataRevision = linkingDataRevision,
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
                                ayahOverrideIds = Array.Empty<int>(),
                                selectedWords = Array.Empty<object>(),
                                automaticWordMatchesEnabled = (bool?)null,
                                manualLinkShape = "independent",
                                descriptions = Array.Empty<object>(),
                            },
                        },
                    },
                },
            });
        createPreflight.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var acceptedPreflight = await ApiEnvelope.ReadDataAsync(createPreflight);
        var preflightId = acceptedPreflight.GetProperty("preflightId").GetGuid();
        var readyPreflight = await PollDataAsync(
            client,
            $"/api/linking/preflights/{preflightId}",
            data => data.GetProperty("status").GetString() == "ready");
        readyPreflight.GetProperty("isBlocked").GetBoolean().Should().BeFalse();
        readyPreflight.GetProperty("isNoOp").GetBoolean().Should().BeFalse();
        readyPreflight.GetProperty("totalAyahs").GetInt32().Should().Be(1);
        var preflightToken = readyPreflight.GetProperty("preflightToken").GetString();
        preflightToken.Should().NotBeNullOrWhiteSpace();
        return new PreparedLinking(preflightId, preflightToken!);
    }

    private static async Task<JsonElement> ResolveSourceAsync(HttpClient client, object descriptor)
    {
        using var response = await client.PostAsJsonAsync(
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ApiEnvelope.ReadDataAsync(response);
    }

    private static async Task<int> CreateTargetDoorAsync(HttpClient client)
    {
        using var sectionResponse = await client.PostAsJsonAsync(
            "/api/abwab/sections",
            new { name = "قسم رحلة الربط الناجحة" });
        sectionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var section = await ApiEnvelope.ReadDataAsync(sectionResponse);

        using var doorResponse = await client.PostAsJsonAsync(
            "/api/abwab/doors",
            new
            {
                sectionId = section.GetProperty("id").GetInt32(),
                parentId = (int?)null,
                name = "باب رحلة الربط الناجحة",
                description = (string?)null,
                representativeAyahText = (string?)null,
                aliases = Array.Empty<string>(),
            });
        doorResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ApiEnvelope.ReadDataAsync(doorResponse)).GetProperty("id").GetInt32();
    }

    private static async Task ProvisionOwnerAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/access/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var owner = await ApiEnvelope.ReadDataAsync(response);
        owner.GetProperty("status").GetString().Should().Be("active");
        owner.GetProperty("isOwner").GetBoolean().Should().BeTrue();
    }

    private static void ConfigureOwner(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(SmokePersonas.OwnerSub));
        client.DefaultRequestHeaders.Add(
            "X-Interactive-Identity-Evidence",
            TestJwtTokens.MintIdentityToken(
                SmokePersonas.OwnerSub,
                FakeExternalUserProfileSource.EmailFor(SmokePersonas.OwnerSub),
                true));
    }

    private static object ManualSourceDescriptor() => new
    {
        kind = "manual-mushaf-ayahs",
        label = "آية الفاتحة الأولى",
        manualAyahs = new[]
        {
            new { verseKey = "1:1", pageNumber = 1, displayHint = "1:1" },
        },
        contextKey = (string?)null,
    };

    private async Task<JsonElement> PollDataAsync(
        HttpClient client,
        string path,
        Func<JsonElement, bool> completed)
    {
        var deadline = DateTimeOffset.UtcNow + CompletionTimeout;
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            last = await ApiEnvelope.ReadDataAsync(response);
            if (completed(last))
            {
                return last;
            }

            await Task.Delay(50);
        }

        var resourceId = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
            ?? "unknown";
        var state = DescribeBusinessState(last);
        var sanitizedLogs = string.Join(" | ", fixture.SanitizedCommandTail());
        throw new TimeoutException(
            $"Timed out waiting for resource {resourceId}; lastState={state}; "
            + $"sanitizedSqlTail={sanitizedLogs}");
    }

    private static string DescribeBusinessState(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return "not-observed";
        }
        var status = data.TryGetProperty("status", out var statusValue)
            ? statusValue.GetString() ?? "unknown"
            : "unknown";
        var stage = data.TryGetProperty("stage", out var stageValue)
            ? stageValue.GetString() ?? "unknown"
            : "unknown";
        return $"status:{status},stage:{stage}";
    }

    private sealed record PreparedLinking(Guid Id, string Token);
}
