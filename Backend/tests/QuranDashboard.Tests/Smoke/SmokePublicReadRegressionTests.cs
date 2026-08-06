using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(SmokeCollection))]
public sealed class SmokePublicReadRegressionTests(SmokeApiFixture fixture)
{
    public static TheoryData<string> PublicReadPaths =>
    [
        "/api/health",
        "/api/dashboard/info",
        "/api/mushaf/pages/1",
        "/api/mushaf/surahs",
        "/api/words/roots",
        "/api/words/lemmas",
        "/api/words/stems",
        "/api/words/unique/tashkeel",
        "/api/abwab/tree",
        "/api/abwab/doors/1/relations",
        "/api/abwab/templates",
        "/api/abwab/templates/1",
    ];

    [Theory]
    [MemberData(nameof(PublicReadPaths))]
    public async Task PublicRead_WithoutToken_DoesNotChallenge(string path)
    {
        // Three of these paths are id-scoped abwab reads whose DerivedStatus is 404 only while the abwab
        // tables are empty, so the restore is this case's precondition, not housekeeping.
        await fixture.ResetAsync();
        var route = SmokeRouteCatalog.Routes.Single(route =>
            route.Method == HttpMethod.Get && route.Path == path);
        using var client = fixture.CreateClientFor(SmokePersona.Anonymous);

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(route.DerivedStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }
}
