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
                           "(a non-GET route sets Method, and ParityOnly if the sweep must not dispatch it)")
            .ToArray();

        uncovered.Should().BeEmpty();
    }

    // Each entry keys by its own Method (GET by default) rather than a hardcoded one, so a non-GET
    // route registered without a matching catalog entry fails parity by name instead of passing by
    // accident. ParityOnly entries are included here — they must be seen by the gate — but excluded
    // from the sweep in SmokeRoutePipelineTests.
    private static IEnumerable<string> CatalogRouteKeys() =>
        SmokeRouteCatalog.Routes.Select(route => RouteKey(route.Method.Method, route.Template));

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
