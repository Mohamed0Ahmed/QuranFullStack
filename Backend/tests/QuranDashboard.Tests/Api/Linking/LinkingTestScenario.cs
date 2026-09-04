using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

internal sealed class LinkingTestScenario(
    ILinkingDataPoller fixture,
    HttpClient client,
    string ownerSub = SmokePersonas.OwnerSub)
{
    public void ConfigureOwner()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(ownerSub));
        client.DefaultRequestHeaders.Add(
            "X-Interactive-Identity-Evidence",
            TestJwtTokens.MintIdentityToken(
                ownerSub,
                FakeExternalUserProfileSource.EmailFor(ownerSub),
                true));
    }

    public async Task ProvisionOwnerAsync()
    {
        using var response = await client.GetAsync("/api/access/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var owner = await ApiEnvelope.ReadDataAsync(response);
        owner.GetProperty("status").GetString().Should().Be("active");
        owner.GetProperty("isOwner").GetBoolean().Should().BeTrue();
    }

    public async Task<int> CreateTargetDoorAsync(string suffix)
    {
        using var sectionResponse = await client.PostAsJsonAsync(
            "/api/abwab/sections",
            new { name = $"قسم حماية الربط {suffix}" });
        sectionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var section = await ApiEnvelope.ReadDataAsync(sectionResponse);

        using var doorResponse = await client.PostAsJsonAsync(
            "/api/abwab/doors",
            new
            {
                sectionId = section.GetProperty("id").GetInt32(),
                parentId = (int?)null,
                name = $"باب حماية الربط {suffix}",
                description = (string?)null,
                representativeAyahText = (string?)null,
                aliases = Array.Empty<string>(),
            });
        doorResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ApiEnvelope.ReadDataAsync(doorResponse)).GetProperty("id").GetInt32();
    }

    public async Task<JsonElement> ResolveSourceAsync()
    {
        using var response = await client.PostAsJsonAsync(
            "/api/linking/sources/resolve-page",
            new
            {
                descriptor = ManualSourceDescriptor(),
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
        var source = await ApiEnvelope.ReadDataAsync(response);
        source.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("verseKey").GetString())
            .Should().Equal("1:1");
        return source;
    }

    public object CreateInlinePreflightRequest(
        Guid preparationKey,
        int doorId,
        long linkingDataRevision,
        string manualLinkShape = "independent") => new
    {
        preparationKey,
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
                    descriptor = ManualSourceDescriptor(),
                    configuration = new
                    {
                        inclusionMode = "all_except",
                        ayahOverrideIds = Array.Empty<int>(),
                        selectedWords = Array.Empty<object>(),
                        automaticWordMatchesEnabled = (bool?)null,
                        manualLinkShape,
                        descriptions = Array.Empty<object>(),
                    },
                },
            },
        },
    };

    public async Task<PreparedLinking> PrepareReadyPreflightAsync(
        int doorId,
        Func<Task>? processPreflight = null)
    {
        var source = await ResolveSourceAsync();
        var currentRevision = source.GetProperty("linkingDataRevision").GetInt64();
        using var response = await client.PostAsJsonAsync(
            "/api/linking/preflights",
            CreateInlinePreflightRequest(Guid.NewGuid(), doorId, currentRevision));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(response);
        var preflightId = accepted.GetProperty("preflightId").GetGuid();

        if (processPreflight is not null)
        {
            await processPreflight();
        }

        var ready = await PollPreflightAsync(preflightId, status => status == "ready");
        ready.GetProperty("isBlocked").GetBoolean().Should().BeFalse();
        ready.GetProperty("isNoOp").GetBoolean().Should().BeFalse();
        ready.GetProperty("totalAyahs").GetInt32().Should().Be(1);
        var token = ready.GetProperty("preflightToken").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        return new PreparedLinking(preflightId, token!);
    }

    public Task<JsonElement> PollPreflightAsync(
        Guid preflightId,
        Func<string?, bool> completed) =>
        fixture.PollDataAsync(
            client,
            $"/api/linking/preflights/{preflightId}",
            "prepared-preflight",
            preflightId.ToString("D"),
            data => completed(data.GetProperty("status").GetString()));

    public Task<JsonElement> PollConfirmationAsync(
        Guid jobId,
        Func<string?, bool> completed) =>
        fixture.PollDataAsync(
            client,
            $"/api/linking/confirmation-jobs/{jobId}",
            "confirmation-job",
            jobId.ToString("D"),
            data => completed(data.GetProperty("status").GetString()));

    public static object ManualSourceDescriptor() => new
    {
        kind = "manual-mushaf-ayahs",
        label = "آية الفاتحة الأولى",
        manualAyahs = new[]
        {
            new { verseKey = "1:1", pageNumber = 1, displayHint = "1:1" },
        },
        contextKey = (string?)null,
    };
}

internal sealed record PreparedLinking(Guid Id, string Token);
