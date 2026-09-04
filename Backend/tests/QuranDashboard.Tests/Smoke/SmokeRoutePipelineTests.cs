using QuranDashboard.Tests.TestSupport.Http;
using QuranDashboard.Tests.Smoke.Data;

namespace QuranDashboard.Tests.Smoke;

// One case per read-only, sweepable catalogued route, so a break names the single route that broke.
// ParityOnly routes are excluded because they may write. Every dispatched case runs anonymously against
// the persistent Test Database Capability through the guarded reader host.
[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeRoutePipelineTests(SmokeDataFixture fixture)
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

        response.StatusCode.Should().Be(route.Seeded?.Status ?? route.PersistentStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }
}
