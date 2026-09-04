using System.Net.Http.Headers;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke.Data;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// The Api/Access suite covers this route too, but against a default-environment host. What is added here
// is the composition itself: the smoke hosts boot under UseEnvironment("Testing"), so base
// appsettings.json is the only configuration file loaded and Swagger is never registered — the route
// table and services a deployed build actually assembles.
[Collection(nameof(MutableDatabaseCollection))]
public sealed class SmokeAuthPipelineTests(AccessTestFixture fixture)
    : SmokeMutableWriterTest(fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task UnknownSub_IsProvisionedPending_AndPersisted()
    {
        using var client = CreateClientFor(SmokePersona.AuthenticatedUnknown);

        using var response = await client.GetAsync(MePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("pending");

        var persisted = await Fixture.GetUserBySubAsync(SmokePersonas.UnknownSub);
        persisted.Should().NotBeNull();
        persisted!.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
        // Provisioning read the identity through the replaced boundary, so no test reaches real Logto.
        ProfileSource.CallsFor(SmokePersonas.UnknownSub).Should().Be(1);
    }

    [Fact]
    public async Task OwnerSub_IsBootstrappedActiveOwner()
    {
        using var client = CreateClientFor(SmokePersona.Owner);

        using var response = await client.GetAsync(MePath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("isOwner").GetBoolean().Should().BeTrue();
    }

}

[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeAuthPipelineReadTests(SmokeDataFixture fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task AnonymousRequest_Returns401_CarryingTheFailureEnvelope()
    {
        using var client = fixture.CreateClient();
        using var response = await client.GetAsync(MePath);

        // The authorization result handler owns the custom challenge envelope, so a bare framework 401
        // must fail here.
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
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

    private async Task AssertTokenRejectedAsync(string token)
    {
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
    }

}
