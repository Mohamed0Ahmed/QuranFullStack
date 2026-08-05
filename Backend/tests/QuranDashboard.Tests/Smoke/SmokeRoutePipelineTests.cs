using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// One case per sweepable catalogued route, so a break names the single route that broke rather than
// collapsing every route into one red test. ParityOnly routes are excluded here on purpose — they write,
// and the sweep's premise (a migrated-but-empty schema stays that way for every other case) would not
// survive dispatching one. Every dispatched case requests anonymously and uses the entry's own Method —
// see SmokeRouteCatalog for how a route's DerivedStatus is derived.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeRoutePipelineTests(SmokeApiFixture fixture)
{
    public static TheoryData<string> CataloguedPaths()
    {
        var paths = new TheoryData<string>();
        foreach (var route in SmokeRouteCatalog.Routes.Where(route => !route.ParityOnly))
        {
            paths.Add(route.Path);
        }

        return paths;
    }

    [Theory]
    [MemberData(nameof(CataloguedPaths))]
    public async Task CataloguedRoute_AnswersItsDerivedStatus_InTheSharedEnvelope(string path)
    {
        var route = SmokeRouteCatalog.ByPath(path);

        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(route.Method, path);
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(
            HttpStatusCode.InternalServerError,
            "a 500 means the handler threw instead of reaching its outcome switch");
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Forbidden,
            "no endpoint in this tree carries an authorization policy");
        if (route.Access.Kind is SmokeRouteAccessKind.Public)
        {
            response.StatusCode.Should().NotBe(
                HttpStatusCode.Unauthorized,
                "authentication is closing a route the catalog records as open to anonymous callers");
        }

        response.StatusCode.Should().Be(route.DerivedStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }
}
