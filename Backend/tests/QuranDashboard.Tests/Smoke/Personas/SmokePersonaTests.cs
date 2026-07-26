using QuranDashboard.Tests.Smoke._Fixtures;
using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke.Personas;

[Collection(nameof(SmokeCollection))]
public sealed class SmokePersonaTests(SmokeApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => SmokeSeed.EnsureSeededAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Missing_token_returns_401_ApiResponse_envelope()
    {
        var response = await fixture.InMemoryClient.GetAsync("api/abwab/templates");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
    }

    [Fact]
    public async Task NoPermissions_persona_gets_403_on_permission_policy()
    {
        var response = await fixture.InMemoryClient.SendAsync(
            SmokeTokens.Get("api/abwab/templates", SmokePersonas.NoPermissions));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Granted_persona_passes_permission_policy()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/abwab/templates", SmokePersonas.Granted)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Owner_persona_passes_SystemOwner_policy()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/security/permissions", SmokePersonas.Owner)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

    [Fact]
    public async Task Access_me_provisions_a_fresh_sub_via_fake_profile_source()
        => (await fixture.InMemoryClient.SendAsync(
                SmokeTokens.Get("api/access/me", SmokePersonas.Fresh)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
}
