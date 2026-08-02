using System.Net.Http.Headers;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// The Api/Access suite covers this route too, but against a default-environment host. What is added here
// is the composition itself: SmokeApiFixture boots under UseEnvironment("Testing"), so base
// appsettings.json is the only configuration file loaded and Swagger is never registered — the route
// table and services a deployed build actually assembles.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeAuthPipelineTests(SmokeApiFixture fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task AnonymousRequest_Returns401_CarryingTheFailureEnvelope()
    {
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var response = await client.GetAsync(MePath);

        // The challenge path is custom — OnChallenge calls HandleResponse() and
        // UnauthorizedRejectionWriter writes the body — so a bare framework 401 must fail here.
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
    }

    [Fact]
    public async Task UnknownSub_IsProvisionedPending_AndPersisted()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClientFor(SmokePersona.AuthenticatedUnknown);

        using var response = await client.GetAsync(MePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("pending");
        data.GetProperty("roleName").ValueKind.Should().Be(JsonValueKind.Null);

        var persisted = await fixture.GetUserBySubAsync(SmokePersonas.UnknownSub);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
        // Provisioning read the identity through the replaced boundary, so no test reaches real Logto.
        fixture.ProfileSource.CallsFor(SmokePersonas.UnknownSub).Should().Be(1);
    }

    [Fact]
    public async Task OwnerSub_IsBootstrappedActiveOwner()
    {
        await fixture.ResetAsync();
        using var client = fixture.CreateClientFor(SmokePersona.Owner);

        using var response = await client.GetAsync(MePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("roleName").GetString().Should().Be(RoleNames.Owner);
    }

    // Two Facts rather than a Theory: the only thing that varies is one Mint argument, so naming each
    // case as data would need a case-key lookup whose whole job is to hide a single expression.
    [Fact]
    public Task TokenSignedWithUntrustedKey_Returns401_CarryingTheFailureEnvelope() =>
        AssertTokenRejectedAsync(
            TestJwtTokens.Mint(SmokePersonas.UnknownSub, signingKey: TestJwtTokens.DifferentKey));

    [Fact]
    public Task ExpiredToken_Returns401_CarryingTheFailureEnvelope() =>
        AssertTokenRejectedAsync(
            TestJwtTokens.Mint(SmokePersonas.UnknownSub, expires: DateTime.UtcNow.AddMinutes(-5)));

    // A fixture self-test, not an API guarantee: the subject is SmokeApiFixture.ResetAsync and the
    // assertion is on the harness's shared role cache. It reaches through ApiServices because the owner's
    // own /me response is re-derived by provisioning and looks identical whether or not a stale role
    // survived, so no HTTP assertion can observe the leak.
    [Fact]
    public async Task ResetAsync_EvictsTheSharedRoleCache_SoNoTestInheritsAPriorRole()
    {
        await fixture.ResetAsync();
        using (var ownerClient = fixture.CreateClientFor(SmokePersona.Owner))
        {
            using var ownerResponse = await ownerClient.GetAsync(MePath);
            ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // RoleClaimsTransformation runs on every authenticated request and caches negatives too, but the
        // owner's request cached its negative BEFORE the bootstrap row existed, and
        // UserProvisioningService.CreateAsync evicts that entry right after the write — so the request
        // alone leaves nothing behind. Priming here is what creates the entry that can leak.
        (await ResolveRoleAsync(SmokePersonas.OwnerSub)).Should().Be(RoleNames.Owner);

        await fixture.ResetAsync();

        // Assumes the primed entry has not aged past CachedUserRoleResolver's 30 s TTL — if it ever did,
        // this guard would start passing vacuously rather than flaking.
        (await ResolveRoleAsync(SmokePersonas.OwnerSub)).Should().BeNull();
    }

    private async Task AssertTokenRejectedAsync(string token)
    {
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
    }

    private async Task<string?> ResolveRoleAsync(string logtoSub)
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IUserRoleResolver>();
        return await resolver.GetActiveRoleNameAsync(logtoSub, CancellationToken.None);
    }
}
