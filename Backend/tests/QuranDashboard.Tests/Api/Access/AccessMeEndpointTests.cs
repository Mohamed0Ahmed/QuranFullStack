using System.Net.Http.Headers;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessMeEndpointTests(AccessTestFixture fixture)
{
    private const string MePath = "/api/access/me";

    [Fact]
    public async Task ValidToken_FirstCall_ProvisionsPendingUserAndReturnsEnvelope()
    {
        await fixture.ResetAsync();
        const string sub = "logto-user-first-login";
        var token = TestJwtTokens.Mint(sub);
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var envelope = document.RootElement;
        envelope.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo(ApiEnvelope.PropertyNames);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeTrue();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.CurrentUserLoaded);

        var data = envelope.GetProperty("data");
        data.GetProperty("sub").GetString().Should().Be(sub);
        // The email is populated from the trusted identity-provider source, never from the caller.
        data.GetProperty("email").GetString().Should().Be(FakeExternalUserProfileSource.EmailFor(sub));
        data.GetProperty("displayName").GetString().Should().Be(FakeExternalUserProfileSource.DisplayNameFor(sub));
        data.GetProperty("status").GetString().Should().Be("pending");
        data.GetProperty("roleId").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetProperty("roleName").ValueKind.Should().Be(JsonValueKind.Null);

        var users = await fixture.GetUsersAsync();
        users.Should().ContainSingle();
        var persisted = users[0];
        persisted.LogtoSub.Should().Be(sub);
        persisted.Email.Should().Be(FakeExternalUserProfileSource.EmailFor(sub));
        persisted.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
    }

    [Fact]
    public async Task ValidToken_RepeatCall_IsIdempotent()
    {
        await fixture.ResetAsync();
        const string sub = "logto-user-repeat-login";
        var token = TestJwtTokens.Mint(sub);
        using var client = fixture.CreateApiClient();

        using var first = await GetMeAsync(client, token);
        using var second = await GetMeAsync(client, token);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get-or-create must short-circuit on the second call: still one row, and the external provider
        // was consulted exactly once (only during the first-login create).
        (await fixture.GetUsersAsync()).Should().ContainSingle();
        fixture.ProfileSource.CallsFor(sub).Should().Be(1);
        fixture.ProfileSource.TotalCalls.Should().Be(1);
    }

    public static TheoryData<string> UnauthorizedCases =>
    [
        "missing-authorization-header",
        "garbage-token",
        "token-signed-by-different-key",
        "expired-token",
        "wrong-audience",
    ];

    [Theory]
    [MemberData(nameof(UnauthorizedCases))]
    public async Task InvalidCredential_Returns401FailureEnvelope(string caseName)
    {
        using var client = fixture.CreateApiClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        if (TokenForCase(caseName) is { } bearer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        using var response = await client.SendAsync(request);

        // The exact ApiResponse envelope, NOT ASP.NET problem-details.
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Unauthorized, ApiMessages.Unauthorized);
    }

    [Fact]
    public async Task BlankEmailFromProvider_Returns500Envelope()
    {
        await fixture.ResetAsync();
        const string sub = "logto-user-blank-email";
        fixture.ProfileSource.ReturnBlankEmailFor(sub);
        var token = TestJwtTokens.Mint(sub);
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, token);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.InternalServerError, ApiMessages.UnexpectedError);

        // Provisioning aborted before any insert, so no partial row leaks.
        (await fixture.GetUsersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task EmailCollidesWithDifferentSub_Returns409ConflictEnvelope()
    {
        await fixture.ResetAsync();
        const string existingSub = "logto-user-me-email-conflict-existing";
        const string newSub = "logto-user-me-email-conflict-new";
        var conflictingEmail = FakeExternalUserProfileSource.EmailFor(existingSub);

        // Seed a pre-existing user under a DIFFERENT sub with the email the new caller's token will
        // present — simulating a subject deleted+recreated in Logto (new sub, same verified email).
        await fixture.InsertUserAsync(new User
        {
            LogtoSub = existingSub,
            Email = conflictingEmail,
            Status = UserStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        fixture.ProfileSource.ReturnEmailFor(newSub, conflictingEmail);
        var token = TestJwtTokens.Mint(newSub);
        using var client = fixture.CreateApiClient();

        using var response = await GetMeAsync(client, token);

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.Conflict, ApiMessages.EmailAlreadyRegistered);

        // The failed insert under the new sub leaves no partial row; only the pre-existing user remains.
        (await fixture.GetUsersAsync()).Should().ContainSingle(u => u.LogtoSub == existingSub);
    }

    public static TheoryData<string> PublicRoutes =>
    [
        // The health check and a real browse endpoint both answer WITHOUT an Authorization header:
        // named role policies are registered but applied to nothing, and there is no fallback policy.
        "/api/health",
        "/api/dashboard/info",
    ];

    [Theory]
    [MemberData(nameof(PublicRoutes))]
    public async Task PublicEndpoint_WithoutToken_StillOk(string route)
    {
        using var client = fixture.CreateApiClient();

        using var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> GetMeAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static string? TokenForCase(string caseName) => caseName switch
    {
        "missing-authorization-header" => null,
        "garbage-token" => "this.is.not-a-valid-jwt",
        "token-signed-by-different-key" => TestJwtTokens.Mint("sub-diff-key", signingKey: TestJwtTokens.DifferentKey),
        "expired-token" => TestJwtTokens.Mint("sub-expired", expires: DateTime.UtcNow.AddHours(-1)),
        "wrong-audience" => TestJwtTokens.Mint("sub-wrong-aud", audience: "https://wrong-audience.example/resource"),
        _ => throw new InvalidOperationException($"Unhandled unauthorized case '{caseName}'."),
    };
}
