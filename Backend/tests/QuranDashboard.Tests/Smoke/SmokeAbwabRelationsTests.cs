using System.Net.Http.Json;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabRelationsTests(SmokeApiFixture fixture)
{
    [Fact]
    public async Task GetRelations_ForAnArchivedAnchor_ReturnsAnEmptyPublicCollection()
    {
        await fixture.ResetAsync();
        using var owner = await fixture.CreateActiveOwnerClientAsync();
        var (sectionId, _) = await SmokeAbwabApi.CreateSectionAsync(owner, "__smoke_relations_section__");
        var (doorId, version) = await SmokeAbwabApi.CreateDoorAsync(owner, "__smoke_relations_archived_anchor__", sectionId);
        using var archive = await owner.SendAsync(JsonRequest(HttpMethod.Delete, $"/api/abwab/doors/{doorId}", new { version }));
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var anonymous = fixture.CreateClientFor(SmokePersona.Anonymous);
        using var response = await anonymous.GetAsync($"/api/abwab/doors/{doorId}/relations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiEnvelope.ReadDataAsync(response)).EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetRelations_ForAnUnknownDoor_ReturnsNotFound()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var response = await client.GetAsync("/api/abwab/doors/999999/relations");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task AddRelations_WithValidTargets_ReturnsCreatedWithTheRelationEnvelope()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (anchorId, targetId) = await CreateDoorPairAsync(client);

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{anchorId}/relations", new
        {
            type = 1,
            direction = (int?)null,
            targetDoorIds = new[] { targetId },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ApiEnvelope.ReadDataAsync(response)).EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task AddRelations_WithoutTargets_ReturnsBadRequest()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (anchorId, _) = await CreateDoorPairAsync(client);

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{anchorId}/relations", new
        {
            type = 1,
            direction = (int?)null,
            targetDoorIds = Array.Empty<int>(),
        });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response,
            HttpStatusCode.BadRequest,
            ApiMessages.AbwabDoorRelationsInvalidRequest);
    }

    [Fact]
    public async Task AddRelations_ForAnUnknownAnchor_ReturnsNotFound()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/999999/relations", new
        {
            type = 1,
            direction = (int?)null,
            targetDoorIds = new[] { 999998 },
        });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task AddRelations_ForAnExistingPair_ReturnsConflict()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (anchorId, targetId) = await CreateDoorPairAsync(client);
        var body = new { type = 1, direction = (int?)null, targetDoorIds = new[] { targetId } };
        using var first = await client.PostAsJsonAsync($"/api/abwab/doors/{anchorId}/relations", body);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{anchorId}/relations", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }

    [Fact]
    public async Task DeleteRelation_ForAnExistingRelation_ReturnsNoContent()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();
        var (anchorId, targetId) = await CreateDoorPairAsync(client);
        using var created = await client.PostAsJsonAsync($"/api/abwab/doors/{anchorId}/relations", new
        {
            type = 1,
            direction = (int?)null,
            targetDoorIds = new[] { targetId },
        });
        var relationId = (await ApiEnvelope.ReadDataAsync(created)).EnumerateArray().Single()
            .GetProperty("id")
            .GetInt32();

        using var response = await client.DeleteAsync($"/api/abwab/relations/{relationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRelation_ForAnUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAsync();
        using var client = await fixture.CreateActiveOwnerClientAsync();

        using var response = await client.DeleteAsync("/api/abwab/relations/999999");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorRelationNotFound);
    }

    private static async Task<(int AnchorId, int TargetId)> CreateDoorPairAsync(HttpClient client)
    {
        var (sectionId, _) = await SmokeAbwabApi.CreateSectionAsync(client, "__smoke_relations_section__");
        var (anchorId, _) = await SmokeAbwabApi.CreateDoorAsync(client, "__smoke_relations_anchor__", sectionId);
        var (targetId, _) = await SmokeAbwabApi.CreateDoorAsync(client, "__smoke_relations_target__", sectionId);
        return (anchorId, targetId);
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body) =>
        new(method, path)
        {
            Content = JsonContent.Create(body),
        };
}
