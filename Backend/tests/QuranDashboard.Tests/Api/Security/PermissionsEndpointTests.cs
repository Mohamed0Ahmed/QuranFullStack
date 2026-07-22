using System.Text;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Tests.Api.Security;

// Integration coverage for the Owner-only permission-administration surface (T052/T061 / SC-007 +
// contracts/permission-admin-api.md): proves the SystemOwner policy, the ApiResponse envelope, and the
// exception -> 409 abwab.* mapping over the real HTTP pipeline against real PostgreSQL — the layer the
// handler-level harness tests bypass.
[Collection(nameof(SecurityApiCollection))]
public sealed class PermissionsEndpointTests(SecurityApiFixture fixture)
{
    private const string ListPath = "/api/security/permissions";
    private const string GrantPath = "/api/security/permissions/grant";
    private const string RevokePath = "/api/security/permissions/revoke";

    public static TheoryData<string> OwnerOnlyRequests => [ListPath, GrantPath, RevokePath];

    [Theory]
    [MemberData(nameof(OwnerOnlyRequests))]
    public async Task NonOwner_IsForbidden_OnEveryEndpoint(string path)
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateApiClient();

        using var request = path == ListPath
            ? new HttpRequestMessage(HttpMethod.Get, path)
            : new HttpRequestMessage(HttpMethod.Post, path) { Content = Body("Role", "Admin", PermissionCatalogue.AttributionManage) };
        request.Headers.Authorization = SecurityApiFixture.Bearer("authenticated-non-owner");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_List_Returns200_WithCatalogueAndAssignments()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-list");
        using var client = fixture.CreateApiClient();

        using var response = await SendAsync(client, HttpMethod.Get, ListPath, "owner-list");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ReadEnvelopeAsync(response);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        envelope.GetProperty("data").GetProperty("catalogue").GetArrayLength().Should().Be(PermissionCatalogue.Codes.Count);
        envelope.GetProperty("data").GetProperty("assignments").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Owner_Grant_Returns200_AndPersistsTheAssignment()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-grant");
        using var client = fixture.CreateApiClient();

        using var response = await SendAsync(
            client, HttpMethod.Post, GrantPath, "owner-grant",
            Body("Role", "Admin", PermissionCatalogue.AttributionManage));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadEnvelopeAsync(response)).GetProperty("isSuccess").GetBoolean().Should().BeTrue();

        var persisted = await fixture.GetAssignmentAsync(PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage);
        persisted.Should().NotBeNull();
        persisted!.IsGranted.Should().BeTrue();
        persisted.Version.Should().Be(1);
    }

    [Fact]
    public async Task Owner_Grant_WithStaleVersion_Returns409_AssignmentStale()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-stale");
        await fixture.SeedGrantedAssignmentAsync(PermissionTargetKind.Role, "Admin", PermissionCatalogue.AttributionManage);
        using var client = fixture.CreateApiClient();

        using var response = await SendAsync(
            client, HttpMethod.Post, GrantPath, "owner-stale",
            Body("Role", "Admin", PermissionCatalogue.AttributionManage, expectedVersion: 99));

        await AssertConflictAsync(response, AbwabConflictCodes.PermissionAssignmentStale);
    }

    [Fact]
    public async Task Owner_Revoke_OfBaseline_Returns409_BaselineLocked()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-baseline");
        using var client = fixture.CreateApiClient();

        using var response = await SendAsync(
            client, HttpMethod.Post, RevokePath, "owner-baseline",
            Body("Role", "Admin", PermissionCatalogue.AttributionView));

        await AssertConflictAsync(response, AbwabConflictCodes.PermissionBaselineLocked);
    }

    [Fact]
    public async Task Owner_Grant_OfSystemOwnerOnlyCode_Returns409_BaselineLocked()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-assignability");
        using var client = fixture.CreateApiClient();

        using var response = await SendAsync(
            client, HttpMethod.Post, GrantPath, "owner-assignability",
            Body("Subject", "ordinary-user", PermissionCatalogue.PermissionAdminister));

        await AssertConflictAsync(response, AbwabConflictCodes.PermissionBaselineLocked);
    }

    [Fact]
    public async Task Owner_RevokeThenRegrant_RoundTrips_UsingTheTombstoneVersion()
    {
        await fixture.ResetAsync();
        await fixture.SeedOwnerAsync("owner-roundtrip");
        using var client = fixture.CreateApiClient();
        const string code = PermissionCatalogue.AttributionManage;

        (await SendAsync(client, HttpMethod.Post, GrantPath, "owner-roundtrip",
            Body("Role", "Admin", code, expectedVersion: 0))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendAsync(client, HttpMethod.Post, RevokePath, "owner-roundtrip",
            Body("Role", "Admin", code, expectedVersion: 1))).StatusCode.Should().Be(HttpStatusCode.OK);

        // The admin List returns the revoked tombstone WITH its current version, so a client can re-grant.
        using var listResponse = await SendAsync(client, HttpMethod.Get, ListPath, "owner-roundtrip");
        var tombstone = (await ReadEnvelopeAsync(listResponse)).GetProperty("data").GetProperty("assignments")
            .EnumerateArray().Single(a => a.GetProperty("permissionCode").GetString() == code);
        tombstone.GetProperty("isGranted").GetBoolean().Should().BeFalse();
        var tombstoneVersion = tombstone.GetProperty("version").GetInt64();
        tombstoneVersion.Should().Be(2);

        using var regrant = await SendAsync(client, HttpMethod.Post, GrantPath, "owner-roundtrip",
            Body("Role", "Admin", code, expectedVersion: tombstoneVersion));
        regrant.StatusCode.Should().Be(HttpStatusCode.OK);

        var persisted = await fixture.GetAssignmentAsync(PermissionTargetKind.Role, "Admin", code);
        persisted!.IsGranted.Should().BeTrue();
        persisted.Version.Should().Be(3);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string ownerSub, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = SecurityApiFixture.Bearer(ownerSub);
        return await client.SendAsync(request);
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response, string expectedCode)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var envelope = await ReadEnvelopeAsync(response);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).Should().Contain(expectedCode);
    }

    private static async Task<JsonElement> ReadEnvelopeAsync(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static StringContent Body(string targetKind, string targetKey, string permissionCode, long expectedVersion = 0)
    {
        var json = JsonSerializer.Serialize(new
        {
            targetKind,
            targetKey,
            permissionCode,
            expectedTimelineGeneration = 0L,
            expectedVersion,
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
