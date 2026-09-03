using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Api.Controllers.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class DeviceSessionLifecycleTests(AccessTestFixture fixture) : AccessMutableWriterTest(fixture)
{
    private const string SessionsPath = "/api/auth/sessions";
    private const string CurrentSessionPath = "/api/auth/sessions/current";
    private const string MePath = "/api/access/me";
    private const string IdentityEvidenceHeader = "X-Interactive-Identity-Evidence";

    [Fact]
    public async Task Bootstrap_SetsSecureCookies_AndAuthenticatesMeWithoutAuthorizationHeader()
    {
        using var client = Fixture.CreateApiClient();

        using var bootstrap = await BootstrapAsync(client, "device-session-cookie");

        bootstrap.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSessionCookies(bootstrap);

        using var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Authorization.Should().BeNull();
        using var me = await client.SendAsync(request);

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(me);
        data.GetProperty("sub").GetString().Should().Be("device-session-cookie");
    }

    [Fact]
    public async Task UnsafeCookieRequest_MissingOrMismatchedCsrf_IsDenied_BeforeMatchedTokenRevokes()
    {
        using var client = Fixture.CreateApiClient();
        using var bootstrap = await BootstrapAsync(client, "device-session-csrf");
        var csrfToken = CookieValue(bootstrap, DeviceSessionAuthentication.CsrfCookieName);
        var sessionToken = CookieValue(bootstrap, DeviceSessionAuthentication.SessionCookieName);

        using var missing = await client.DeleteAsync(CurrentSessionPath);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            missing,
            HttpStatusCode.Forbidden,
            ApiMessages.InvalidCsrfToken);

        using var mismatchedRequest = new HttpRequestMessage(HttpMethod.Delete, CurrentSessionPath);
        mismatchedRequest.Headers.Add(DeviceSessionAuthentication.CsrfHeaderName, "mismatched-csrf-token");
        using var mismatched = await client.SendAsync(mismatchedRequest);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            mismatched,
            HttpStatusCode.Forbidden,
            ApiMessages.InvalidCsrfToken);

        using var stillAuthenticated = await client.GetAsync(MePath);
        stillAuthenticated.StatusCode.Should().Be(HttpStatusCode.OK);

        using var matchedRequest = new HttpRequestMessage(HttpMethod.Delete, CurrentSessionPath);
        matchedRequest.Headers.Add(DeviceSessionAuthentication.CsrfHeaderName, csrfToken);
        using var matched = await client.SendAsync(matchedRequest);
        matched.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var afterRevoke = await client.GetAsync(MePath);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            afterRevoke,
            HttpStatusCode.Unauthorized,
            ApiMessages.Unauthorized);

        using var replayClient = Fixture.CreateApiClient();
        using var replayRequest = CookieBackedMeRequest(sessionToken);
        using var replayedRevokedSession = await replayClient.SendAsync(replayRequest);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            replayedRevokedSession,
            HttpStatusCode.Unauthorized,
            ApiMessages.Unauthorized);
    }

    [Fact]
    public async Task Bootstrap_WithPreviousCookie_ReplacesAndInvalidatesPreviousSession()
    {
        using var bootstrapClient = Fixture.CreateApiClient();

        using var first = await BootstrapAsync(bootstrapClient, "device-session-replacement");
        var firstToken = CookieValue(first, DeviceSessionAuthentication.SessionCookieName);
        using var second = await BootstrapAsync(bootstrapClient, "device-session-replacement");
        var secondToken = CookieValue(second, DeviceSessionAuthentication.SessionCookieName);

        secondToken.Should().NotBe(firstToken);

        using var oldSessionClient = Fixture.CreateApiClient();
        using var oldRequest = CookieBackedMeRequest(firstToken);
        using var oldResponse = await oldSessionClient.SendAsync(oldRequest);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            oldResponse,
            HttpStatusCode.Unauthorized,
            ApiMessages.Unauthorized);

        using var currentSessionClient = Fixture.CreateApiClient();
        using var currentRequest = CookieBackedMeRequest(secondToken);
        using var currentResponse = await currentSessionClient.SendAsync(currentRequest);
        currentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CookieBackedAccess_ExpiresAtTheControllableClockBoundary()
    {
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        using var factory = Fixture.CreateApiFactory(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
        });
        using var client = Fixture.CreateApiClient(factory);
        using var bootstrap = await BootstrapAsync(client, "device-session-expiry");
        bootstrap.StatusCode.Should().Be(HttpStatusCode.OK);

        clock.Advance(TimeSpan.FromDays(90));

        using var expired = await client.GetAsync(MePath);
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            expired,
            HttpStatusCode.Unauthorized,
            ApiMessages.Unauthorized);
    }

    private static async Task<HttpResponseMessage> BootstrapAsync(HttpClient client, string subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SessionsPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(subject));
        request.Headers.Add(
            IdentityEvidenceHeader,
            TestJwtTokens.MintIdentityToken(
                subject,
                FakeExternalUserProfileSource.EmailFor(subject),
                true));
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CookieBackedMeRequest(string sessionToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MePath);
        request.Headers.Add(
            "Cookie",
            $"{DeviceSessionAuthentication.SessionCookieName}={sessionToken}");
        return request;
    }

    private static void AssertSessionCookies(HttpResponseMessage response)
    {
        var session = CookieHeader(response, DeviceSessionAuthentication.SessionCookieName).ToLowerInvariant();
        session.Should().Contain("; secure");
        session.Should().Contain("; httponly");
        session.Should().Contain("; samesite=lax");
        session.Should().Contain("; path=/api");

        var csrf = CookieHeader(response, DeviceSessionAuthentication.CsrfCookieName).ToLowerInvariant();
        csrf.Should().Contain("; secure");
        csrf.Should().NotContain("; httponly");
        csrf.Should().Contain("; samesite=lax");
        csrf.Should().Contain("; path=/");
    }

    private static string CookieValue(HttpResponseMessage response, string name)
    {
        var header = CookieHeader(response, name);
        var pair = header.Split(';', 2)[0];
        return Uri.UnescapeDataString(pair[(name.Length + 1)..]);
    }

    private static string CookieHeader(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith($"{name}=", StringComparison.Ordinal));

    private sealed class AdjustableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow = utcNow.Add(duration);
    }
}
