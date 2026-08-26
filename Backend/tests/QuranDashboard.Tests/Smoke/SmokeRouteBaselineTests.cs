namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeRouteBaselineTests
{
    [Fact]
    public void CurrentRouteBaseline_RecordsCountMethodPathAndAccessShape()
    {
        var snapshot = SmokeRouteCatalog.Routes
            .Select(route => (route.Method.Method, route.Template, route.Access.Kind, route.Access.PermissionCode))
            .ToArray();

        snapshot.Should().HaveCount(114);
        snapshot.Count(route => route.Kind is SmokeRouteAccessKind.AuthenticatedOnly).Should().Be(3);
        snapshot.Count(route => route.Kind is SmokeRouteAccessKind.Permission).Should().Be(23);
        snapshot.Count(route => route.Kind is SmokeRouteAccessKind.OwnerOnly).Should().Be(30);
        snapshot.Should().Contain(("GET", "api/access/me", SmokeRouteAccessKind.AuthenticatedOnly, null));
        snapshot.Should().Contain(("POST", "api/auth/sessions", SmokeRouteAccessKind.AuthenticatedOnly, null));
        snapshot.Should().Contain(("DELETE", "api/auth/sessions/current", SmokeRouteAccessKind.AuthenticatedOnly, null));
        snapshot.Should().Contain(("GET", "api/access/users", SmokeRouteAccessKind.OwnerOnly, null));
        snapshot.Should().OnlyContain(route =>
            route.Kind == SmokeRouteAccessKind.Public
            || route.Kind == SmokeRouteAccessKind.AuthenticatedOnly
            || route.Kind == SmokeRouteAccessKind.Permission
            || route.Kind == SmokeRouteAccessKind.OwnerOnly);
        SmokeRouteCatalog.Routes.Where(route =>
                route.Method.Method == "POST"
                || route.Method.Method == "PUT"
                || route.Method.Method == "PATCH"
                || route.Method.Method == "DELETE")
            .Should().OnlyContain(route =>
                route.Access.Kind == SmokeRouteAccessKind.Permission
                || route.Access.Kind == SmokeRouteAccessKind.OwnerOnly
                || route.Template == "api/auth/sessions"
                || route.Template == "api/auth/sessions/current");
        SmokeRouteCatalog.Routes
            .Where(route => route.Method == HttpMethod.Get && route.Access.Kind == SmokeRouteAccessKind.Public)
            .Should()
            .OnlyContain(route => route.Access.Kind == SmokeRouteAccessKind.Public);
    }

    [Fact]
    public void PublicReadBaseline_ExplicitlyKeepsTheContentAndHealthGetsAnonymous()
    {
        var publicReadPaths = new[]
        {
            "/api/abwab/tree",
            "/api/abwab/doors/1/links/snapshot",
            "/api/abwab/doors/1/links?page=1&pageSize=100",
            "/api/abwab/doors/1/links/1/ayahs?page=1&pageSize=100",
            "/api/abwab/doors/1/relations",
            "/api/abwab/templates",
            "/api/abwab/templates/1",
            "/api/health",
            "/api/dashboard/info",
            "/api/mushaf/pages/1",
            "/api/mushaf/surahs",
            "/api/words/roots",
            "/api/words/lemmas",
            "/api/words/stems",
            "/api/words/unique/tashkeel",
        };

        publicReadPaths.Should().OnlyContain(path =>
            SmokeRouteCatalog.Routes.Single(route => route.Method == HttpMethod.Get && route.Path == path)
                .Access.Kind == SmokeRouteAccessKind.Public);
    }
}
