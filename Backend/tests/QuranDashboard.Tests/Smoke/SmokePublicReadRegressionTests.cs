using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(SmokeCollection))]
public sealed class SmokePublicReadRegressionTests(SmokeApiFixture fixture)
{
    public static TheoryData<string> PublicReadPaths()
    {
        var paths = new TheoryData<string>();
        foreach (var route in SmokeRouteCatalog.Routes
                     .Where(route => route.Method == HttpMethod.Get && route.Access.Kind is SmokeRouteAccessKind.Public)
                     .OrderBy(route => route.Path, StringComparer.Ordinal))
        {
            paths.Add(route.Path);
        }

        return paths;
    }

    public static TheoryData<string, string> PublicAbwabReadPersonaPaths() => new()
    {
        { nameof(SmokePersona.Pending), "/api/abwab/tree" },
        { nameof(SmokePersona.Pending), "/api/abwab/doors/1/relations" },
        { nameof(SmokePersona.Pending), "/api/abwab/templates" },
        { nameof(SmokePersona.Pending), "/api/abwab/templates/1" },
        { nameof(SmokePersona.Disabled), "/api/abwab/tree" },
        { nameof(SmokePersona.Disabled), "/api/abwab/doors/1/relations" },
        { nameof(SmokePersona.Disabled), "/api/abwab/templates" },
        { nameof(SmokePersona.Disabled), "/api/abwab/templates/1" },
    };

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

    [Theory]
    [MemberData(nameof(PublicAbwabReadPersonaPaths))]
    public async Task PublicAbwabRead_WithAnInactiveToken_DoesNotBecomeForbidden(string personaName, string path)
    {
        await fixture.ResetAsync();
        await fixture.SeedAuthorizationPersonasAsync(
            AbwabPermissions.Sections.Create,
            AbwabPermissions.Sections.Edit);
        var route = SmokeRouteCatalog.Routes.Single(route => route.Method == HttpMethod.Get && route.Path == path);
        var persona = Enum.Parse<SmokePersona>(personaName);
        using var client = fixture.CreateClientFor(persona);

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(route.DerivedStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }
}
