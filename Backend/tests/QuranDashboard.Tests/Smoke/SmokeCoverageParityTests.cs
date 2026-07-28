using Microsoft.AspNetCore.Routing;

namespace QuranDashboard.Tests.Smoke;

// The lock between SmokeRouteCatalog and the route table the API actually composes. Both directions are
// asserted, so neither adding an endpoint without a catalog entry nor deleting an entry can pass.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeCoverageParityTests(SmokeApiFixture fixture)
{
    [Fact]
    public void EveryCatalogEntry_IsARegisteredRoute()
    {
        var live = LiveRouteKeys();

        var missing = CatalogRouteKeys()
            .Where(key => !live.Contains(key))
            .Select(key => $"catalog entry '{key}' is not a registered route — renamed, deleted, or its constraint changed; update or remove the entry")
            .ToArray();

        missing.Should().BeEmpty();
    }

    [Fact]
    public void EveryRegisteredRoute_HasACatalogEntry()
    {
        var catalogued = CatalogRouteKeys().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var uncovered = LiveRouteKeys()
            .Where(key => !catalogued.Contains(key))
            .Select(key => $"registered route '{key}' has no SmokeRouteCatalog entry — add one in the same change " +
                           "(a non-GET route also needs SmokeRoute to carry its method)")
            .ToArray();

        uncovered.Should().BeEmpty();
    }

    // Every catalogued route is a GET, so the method is supplied here rather than stored 48 times. A
    // non-GET endpoint would still be keyed by its own method on the live side and fail parity by name,
    // which is when the method earns a place on SmokeRoute.
    private static IEnumerable<string> CatalogRouteKeys() =>
        SmokeRouteCatalog.Routes.Select(route => RouteKey(HttpMethod.Get.Method, route.Template));

    // An endpoint with no HttpMethodMetadata is keyed under this rather than dropped: every route the
    // host composes has to reach the comparison, because an unexplained exclusion filter is how a parity
    // gate rots into a gate that passes on nothing.
    private const string UnconstrainedMethod = "ANY";

    // IReadOnlySet, not IReadOnlyCollection: Contains then resolves to a real interface member carrying
    // this set's comparer, instead of falling through to Enumerable.Contains and depending on its
    // ICollection fast path to reach the comparer by accident.
    private IReadOnlySet<string> LiveRouteKeys() =>
        fixture.ApiServices.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            // The one exclusion here: an endpoint that is not a RouteEndpoint has no RoutePattern, so
            // there is nothing to key it by. None exist in this host today.
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint =>
                (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [UnconstrainedMethod])
                    .Select(method => RouteKey(method, endpoint.RoutePattern.RawText ?? string.Empty)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Both sides are keyed "<METHOD> <template>", the template being EndpointDataSource's own RawText
    // with the leading slash trimmed (attribute routing reports it without one). Casing is left to the
    // sets' ordinal-ignore-case comparer so there is one normalization mechanism, not two; route
    // constraints stay part of the key, so relaxing {id:int} to {id} is a mismatch.
    private static string RouteKey(string method, string template) =>
        $"{method} {template.TrimStart('/')}";
}
