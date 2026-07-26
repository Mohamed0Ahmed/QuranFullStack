using QuranDashboard.Tests.Smoke._Fixtures;
using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke.Pipeline;

// The assertion contract proves routing + authorization + model binding + serialization for every
// registered route, not business outcomes: seeded state accumulates across cases, so domain
// 400/404/409 are tolerated where noted — auth rejections and 5xx never are.
[Collection(nameof(SmokeCollection))]
public sealed class PipelineSmokeTests(SmokeApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => SmokeSeed.EnsureSeededAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    public static TheoryData<string> ProtectedRouteKeys() =>
        Keys(routeCase => routeCase.Auth.Kind
            is SmokeAuthKind.Authenticated or SmokeAuthKind.Permission or SmokeAuthKind.SystemOwner);

    public static TheoryData<string> AnonymousRouteKeys() =>
        Keys(routeCase => routeCase.Auth.Kind is SmokeAuthKind.Anonymous);

    public static TheoryData<string> TreeRouteKeys() =>
        Keys(routeCase => routeCase.Auth.Kind is SmokeAuthKind.TreeRedacted);

    public static TheoryData<string> PolicyRouteKeys() =>
        Keys(routeCase => routeCase.Auth.Kind
            is SmokeAuthKind.Permission or SmokeAuthKind.SystemOwner or SmokeAuthKind.TreeRedacted);

    public static TheoryData<string> AllRouteKeys() => Keys(_ => true);

    public static TheoryData<string> InvalidBodyRouteKeys() =>
        Keys(routeCase => routeCase.InvalidBody is not null);

    [Theory]
    [MemberData(nameof(ProtectedRouteKeys))]
    public async Task Unauthenticated_protected_route_returns_401_envelope(string key)
    {
        using var response = await SendAsync(key, sub: null, BodyKind.Valid);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, key);
        await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
    }

    [Theory]
    [MemberData(nameof(AnonymousRouteKeys))]
    public async Task Anonymous_route_serves_unauthenticated_requests(string key)
    {
        using var response = await SendAsync(key, sub: null, BodyKind.None);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, key);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, key);
        ((int)response.StatusCode).Should().BeLessThan(500, key);
    }

    [Theory]
    [MemberData(nameof(TreeRouteKeys))]
    public async Task Tree_route_returns_in_body_403_for_unauthenticated_callers(string key)
    {
        using var response = await SendAsync(key, sub: null, BodyKind.None);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, key);
        await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
    }

    [Theory]
    [MemberData(nameof(PolicyRouteKeys))]
    public async Task NoPermissions_persona_is_forbidden(string key)
    {
        using var response = await SendAsync(key, SmokePersonas.NoPermissions, BodyKind.Valid);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, key);
        if (SmokeRouteCatalog.Cases[key].Auth.Kind is SmokeAuthKind.TreeRedacted)
        {
            await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
        }
    }

    [Theory]
    [MemberData(nameof(AllRouteKeys))]
    public async Task Authorized_request_passes_the_pipeline(string key)
    {
        var routeCase = SmokeRouteCatalog.Cases[key];
        using var response = await SendAsync(key, AuthorizedSubFor(routeCase), BodyKind.Valid);

        if (routeCase.Auth.Kind is SmokeAuthKind.TreeRedacted)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, key);
            return;
        }

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, key);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, key);
        response.StatusCode.Should().NotBe(HttpStatusCode.UnsupportedMediaType, key);
        ((int)response.StatusCode).Should().BeLessThan(500, key);
    }

    [Theory]
    [MemberData(nameof(InvalidBodyRouteKeys))]
    public async Task Invalid_body_returns_400_envelope_not_500(string key)
    {
        var routeCase = SmokeRouteCatalog.Cases[key];
        using var response = await SendAsync(key, AuthorizedSubFor(routeCase), BodyKind.Invalid);

        ((int)response.StatusCode).Should().BeLessThan(500, $"{key} must reject malformed input, not crash");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, key);
        await ApiEnvelopeAssertions.AssertFailureEnvelopeAsync(response);
    }

    [Fact]
    public async Task Kestrel_sentinel_serves_anonymous_reads()
    {
        (await fixture.KestrelClient.GetAsync("api/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await fixture.KestrelClient.GetAsync("api/mushaf/surahs")).StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kestrel_sentinel_enforces_authorization()
    {
        using var unauthenticated = await fixture.KestrelClient.GetAsync("api/abwab/templates");
        unauthenticated.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var granted = await fixture.KestrelClient.SendAsync(
            SmokeTokens.Get("api/abwab/templates", SmokePersonas.Granted));
        granted.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Kestrel_sentinel_binds_and_rejects_template_bodies()
    {
        using var valid = await fixture.KestrelClient.SendAsync(SmokeTokens.Build(
            HttpMethod.Post, "api/abwab/templates", SmokePersonas.Granted,
            """{"name":"قالب كسترل","description":null,"expectedTimelineGeneration":0}"""));
        valid.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        valid.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        ((int)valid.StatusCode).Should().BeLessThan(500);

        using var invalid = await fixture.KestrelClient.SendAsync(SmokeTokens.Build(
            HttpMethod.Post, "api/abwab/templates", SmokePersonas.Granted,
            """{"description":null,"expectedTimelineGeneration":0}"""));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private enum BodyKind
    {
        None,
        Valid,
        Invalid,
    }

    private async Task<HttpResponseMessage> SendAsync(string key, string? sub, BodyKind bodyKind)
    {
        var routeCase = SmokeRouteCatalog.Cases[key];
        var body = bodyKind switch
        {
            BodyKind.Valid => routeCase.ValidBody?.Invoke(SmokeSeed.Context),
            BodyKind.Invalid => routeCase.InvalidBody?.Invoke(SmokeSeed.Context),
            _ => null,
        };

        using var request = SmokeTokens.Build(
            routeCase.Method, routeCase.ConcretePath(SmokeSeed.Context), sub, body);
        return await fixture.InMemoryClient.SendAsync(request);
    }

    private static string? AuthorizedSubFor(SmokeRouteCase routeCase) => routeCase.Auth.Kind switch
    {
        SmokeAuthKind.SystemOwner => SmokePersonas.Owner,
        SmokeAuthKind.Anonymous => null,
        _ => SmokePersonas.Granted,
    };

    private static TheoryData<string> Keys(Func<SmokeRouteCase, bool> predicate)
    {
        var data = new TheoryData<string>();
        foreach (var routeCase in SmokeRouteCatalog.Cases.Values.Where(predicate))
        {
            data.Add(routeCase.Key);
        }

        return data;
    }
}
