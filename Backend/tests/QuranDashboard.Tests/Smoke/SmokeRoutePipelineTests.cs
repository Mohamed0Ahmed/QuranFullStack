using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// One case per catalogued route, so a break names the single route that broke rather than collapsing 48
// routes into one red test. Every case requests anonymously and reads only, which is what keeps the
// migrated-but-empty schema the expectations are derived against true for the whole sweep — see
// SmokeRouteCatalog for how a route's DerivedStatus is derived.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeRoutePipelineTests(SmokeApiFixture fixture)
{
    public static TheoryData<string> CataloguedPaths()
    {
        var paths = new TheoryData<string>();
        foreach (var route in SmokeRouteCatalog.Routes)
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
        using var response = await client.GetAsync(path);

        response.StatusCode.Should().NotBe(
            HttpStatusCode.InternalServerError,
            "a 500 means the handler threw instead of reaching its outcome switch");
        response.StatusCode.Should().NotBe(
            HttpStatusCode.Forbidden,
            "no endpoint in this tree carries an authorization policy");
        if (route.Access is SmokeRouteAccess.Open)
        {
            response.StatusCode.Should().NotBe(
                HttpStatusCode.Unauthorized,
                "authentication is closing a route the catalog records as open to anonymous callers");
        }

        response.StatusCode.Should().Be(route.DerivedStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }
}
