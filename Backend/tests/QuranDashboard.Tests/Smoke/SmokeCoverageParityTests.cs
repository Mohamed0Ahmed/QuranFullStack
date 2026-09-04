using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.Smoke.Data;

namespace QuranDashboard.Tests.Smoke;

// The lock between SmokeRouteCatalog and the route table the API actually composes. Both directions are
// asserted, so neither adding an endpoint without a catalog entry nor deleting an entry can pass.
[Collection(nameof(SmokeDataCollection))]
public sealed class SmokeCoverageParityTests(SmokeDataFixture fixture)
{
    private static readonly IReadOnlyList<AbwabWritePermission> AbwabWriteMatrix =
    [
        new("POST", "api/abwab/sections", AbwabPermissions.Sections.Create),
        new("PUT", "api/abwab/sections/{id:int}", AbwabPermissions.Sections.Edit),
        new("DELETE", "api/abwab/sections/{id:int}", AbwabPermissions.Sections.Delete),
        new("POST", "api/abwab/sections/{id:int}/order", AbwabPermissions.Sections.Reorder),
        new("POST", "api/abwab/doors", AbwabPermissions.Doors.Create),
        new("PUT", "api/abwab/doors/{id:int}", AbwabPermissions.Doors.Edit),
        new("POST", "api/abwab/doors/{id:int}/move", AbwabPermissions.Doors.Move),
        new("POST", "api/abwab/doors/{id:int}/order", AbwabPermissions.Doors.Reorder),
        new("POST", "api/abwab/doors/bulk-move", AbwabPermissions.Doors.Move),
        new("POST", "api/abwab/doors/bulk-archive", AbwabPermissions.Doors.Archive),
        new("DELETE", "api/abwab/doors/{id:int}", AbwabPermissions.Doors.Archive),
        new("POST", "api/abwab/doors/{id:int}/restore", AbwabPermissions.Doors.Restore),
        new("POST", "api/abwab/doors/{doorId:int}/relations", AbwabPermissions.Relations.Create),
        new("DELETE", "api/abwab/relations/{relationId:int}", AbwabPermissions.Relations.Delete),
        new("POST", "api/abwab/doors/{targetDoorId:int}/inclusions", AbwabPermissions.Inclusions.Create),
        new(
            "DELETE",
            "api/abwab/doors/{targetDoorId:int}/inclusions/{inclusionId:int}",
            AbwabPermissions.Inclusions.Delete),
        new("POST", "api/abwab/templates", AbwabPermissions.Templates.Create),
        new("DELETE", "api/abwab/templates/{templateId:int}", AbwabPermissions.Templates.Delete),
        new("POST", "api/abwab/templates/{templateId:int}/apply", AbwabPermissions.Templates.Apply),
        new("POST", "api/abwab/templates/{templateId:int}/nodes", AbwabPermissions.TemplateNodes.Create),
        new("PUT", "api/abwab/template-nodes/{nodeId:int}", AbwabPermissions.TemplateNodes.Edit),
        new("POST", "api/abwab/template-nodes/{nodeId:int}/order", AbwabPermissions.TemplateNodes.Reorder),
        new("DELETE", "api/abwab/template-nodes/{nodeId:int}", AbwabPermissions.TemplateNodes.Delete),
    ];

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

    [Fact]
    public void EveryCataloguedRoute_HasTheExactAccessClassification()
    {
        var live = LiveRoutes();

        foreach (var route in SmokeRouteCatalog.Routes)
        {
            var key = RouteKey(route.Method.Method, route.Template);
            AssertAccessClassification(live[key], route, key);
        }
    }

    [Fact]
    public void EveryPublicGet_HasNoAuthorizationMetadata()
    {
        var live = LiveRoutes();

        foreach (var route in SmokeRouteCatalog.Routes.Where(route =>
                     route.Method.Method == HttpMethods.Get && route.Access.Kind is SmokeRouteAccessKind.Public))
        {
            var key = RouteKey(route.Method.Method, route.Template);
            var endpoint = live[key];

            endpoint.Metadata.OfType<IAuthorizeData>().Should().BeEmpty(key);
            endpoint.Metadata.OfType<RequirePermissionAttribute>().Should().BeEmpty(key);
            endpoint.Metadata.OfType<RequireOwnerAttribute>().Should().BeEmpty(key);
        }
    }

    [Fact]
    public void LiveAbwabWrites_MatchTheCompletePermissionMatrix()
    {
        var actual = LiveRoutes()
            .Where(pair =>
                IsAbwabUnsafeRoute(pair.Key)
                && !pair.Value.Metadata.OfType<RequireOwnerAttribute>().Any())
            .Select(pair =>
            {
                var permission = pair.Value.Metadata.OfType<RequirePermissionAttribute>().Single();
                return new AbwabWritePermission(
                    MethodFromRouteKey(pair.Key),
                    TemplateFromRouteKey(pair.Key),
                    permission.PermissionCode);
            })
            .ToArray();

        actual.Should().BeEquivalentTo(AbwabWriteMatrix);
    }

    private static void AssertAccessClassification(RouteEndpoint endpoint, SmokeRoute route, string key)
    {
        var authorization = endpoint.Metadata.OfType<IAuthorizeData>().ToArray();
        var permissions = endpoint.Metadata.OfType<RequirePermissionAttribute>().ToArray();
        var owners = endpoint.Metadata.OfType<RequireOwnerAttribute>().ToArray();

        switch (route.Access.Kind)
        {
            case SmokeRouteAccessKind.Public:
                authorization.Should().BeEmpty(key);
                break;
            case SmokeRouteAccessKind.AuthenticatedOnly:
                permissions.Should().BeEmpty(key);
                owners.Should().BeEmpty(key);
                authorization.Should().ContainSingle(key);
                authorization.Should().OnlyContain(metadata =>
                    metadata.GetType() != typeof(RequirePermissionAttribute)
                    && metadata.GetType() != typeof(RequireOwnerAttribute), key);
                break;
            case SmokeRouteAccessKind.Permission:
                permissions.Should().ContainSingle(key)
                    .Which.PermissionCode.Should().Be(route.Access.PermissionCode, key);
                owners.Should().BeEmpty(key);
                authorization.Should().OnlyContain(
                    metadata => metadata.GetType() == typeof(RequirePermissionAttribute), key);
                break;
            case SmokeRouteAccessKind.OwnerOnly:
                permissions.Should().BeEmpty(key);
                owners.Should().ContainSingle(key);
                authorization.Should().OnlyContain(
                    metadata => metadata.GetType() == typeof(RequireOwnerAttribute), key);
                break;
            default:
                throw new InvalidOperationException($"Unknown access kind '{route.Access.Kind}'.");
        }

        if (IsUnsafeMethod(route.Method.Method))
        {
            UnsafeEndpointMetadataValidator.ValidateEndpoint(endpoint).IsValid.Should().BeTrue(key);
        }
    }

    private IReadOnlyDictionary<string, RouteEndpoint> LiveRoutes()
    {
        var pairs = fixture.ApiServices.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint =>
                (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [UnconstrainedMethod])
                    .Select(method => new KeyValuePair<string, RouteEndpoint>(
                        RouteKey(method, endpoint.RoutePattern.RawText ?? string.Empty),
                        endpoint)))
            .ToArray();
        var duplicateKeys = pairs.GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        duplicateKeys.Should().BeEmpty();
        return pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsAbwabUnsafeRoute(string key) =>
        IsUnsafeMethod(MethodFromRouteKey(key))
        && TemplateFromRouteKey(key).StartsWith("api/abwab/", StringComparison.OrdinalIgnoreCase);

    private static bool IsUnsafeMethod(string method) =>
        string.Equals(method, HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, HttpMethods.Put, StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, HttpMethods.Patch, StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);

    private static string MethodFromRouteKey(string key) => key[..key.IndexOf(' ')];

    private static string TemplateFromRouteKey(string key) => key[(key.IndexOf(' ') + 1)..];

    // Both sides are keyed "<METHOD> <template>", the template being EndpointDataSource's own RawText
    // with the leading slash trimmed (attribute routing reports it without one). Casing is left to the
    // sets' ordinal-ignore-case comparer so there is one normalization mechanism, not two; route
    // constraints stay part of the key, so relaxing {id:int} to {id} is a mismatch.
    private static string RouteKey(string method, string template) =>
        $"{method} {template.TrimStart('/')}";

    private sealed record AbwabWritePermission(string Method, string Template, string PermissionCode);
}
