using System.Net.Http.Json;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class SmokeAbwabWriteAuthorizationTests(AccessTestFixture fixture)
    : SmokeMutableWriterTest(fixture)
{
    private static readonly IReadOnlyDictionary<string, SmokeAbwabWriteAuthorizationRoute> Routes =
        new SmokeAbwabWriteAuthorizationRoute[]
        {
            new(
                "POST api/abwab/sections",
                HttpMethod.Post,
                "/api/abwab/sections",
                AbwabPermissions.Sections.Create,
                AbwabPermissions.Sections.Edit,
                HttpStatusCode.BadRequest,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/sections", new { name = " " })),
            new(
                "PUT api/abwab/sections/{id}",
                HttpMethod.Put,
                "/api/abwab/sections/999999",
                AbwabPermissions.Sections.Edit,
                AbwabPermissions.Sections.Create,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Put, "/api/abwab/sections/999999", new { name = "__smoke__", version = 0u })),
            new(
                "DELETE api/abwab/sections/{id}",
                HttpMethod.Delete,
                "/api/abwab/sections/999999",
                AbwabPermissions.Sections.Delete,
                AbwabPermissions.Sections.Edit,
                HttpStatusCode.NotFound,
                () => new HttpRequestMessage(HttpMethod.Delete, "/api/abwab/sections/999999")),
            new(
                "POST api/abwab/sections/{id}/order",
                HttpMethod.Post,
                "/api/abwab/sections/999999/order",
                AbwabPermissions.Sections.Reorder,
                AbwabPermissions.Sections.Edit,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/sections/999999/order", new { position = 1, version = 0u })),
            new(
                "POST api/abwab/doors",
                HttpMethod.Post,
                "/api/abwab/doors",
                AbwabPermissions.Doors.Create,
                AbwabPermissions.Doors.Edit,
                HttpStatusCode.BadRequest,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors", new
                {
                    sectionId = (int?)null,
                    parentId = (int?)null,
                    name = "__smoke__",
                    aliases = Array.Empty<string>(),
                })),
            new(
                "PUT api/abwab/doors/{id}",
                HttpMethod.Put,
                "/api/abwab/doors/999999",
                AbwabPermissions.Doors.Edit,
                AbwabPermissions.Doors.Create,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Put, "/api/abwab/doors/999999", new
                {
                    name = "__smoke__",
                    description = (string?)null,
                    representativeAyahText = (string?)null,
                    aliases = Array.Empty<string>(),
                    version = 0u,
                })),
            new(
                "POST api/abwab/doors/{id}/move",
                HttpMethod.Post,
                "/api/abwab/doors/999999/move",
                AbwabPermissions.Doors.Move,
                AbwabPermissions.Doors.Archive,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/999999/move", new
                {
                    targetSectionId = (int?)null,
                    targetParentId = (int?)null,
                    version = 0u,
                })),
            new(
                "POST api/abwab/doors/{id}/order",
                HttpMethod.Post,
                "/api/abwab/doors/999999/order",
                AbwabPermissions.Doors.Reorder,
                AbwabPermissions.Doors.Move,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/999999/order", new
                {
                    position = 1,
                    scope = 1,
                    version = 0u,
                })),
            new(
                "POST api/abwab/doors/bulk-move",
                HttpMethod.Post,
                "/api/abwab/doors/bulk-move",
                AbwabPermissions.Doors.Move,
                AbwabPermissions.Doors.Archive,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/bulk-move", new
                {
                    doors = new[] { new { doorId = 999999, version = 0u } },
                    targetSectionId = 999998,
                    targetParentId = (int?)null,
                })),
            new(
                "POST api/abwab/doors/bulk-archive",
                HttpMethod.Post,
                "/api/abwab/doors/bulk-archive",
                AbwabPermissions.Doors.Archive,
                AbwabPermissions.Doors.Move,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/bulk-archive", new
                {
                    doors = new[] { new { doorId = 999999, version = 0u } },
                })),
            new(
                "DELETE api/abwab/doors/{id}",
                HttpMethod.Delete,
                "/api/abwab/doors/999999",
                AbwabPermissions.Doors.Archive,
                AbwabPermissions.Doors.Move,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Delete, "/api/abwab/doors/999999", new { version = 0u })),
            new(
                "POST api/abwab/doors/{id}/restore",
                HttpMethod.Post,
                "/api/abwab/doors/999999/restore",
                AbwabPermissions.Doors.Restore,
                AbwabPermissions.Doors.Archive,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/999999/restore", new
                {
                    sectionId = (int?)null,
                    version = 0u,
                })),
            new(
                "POST api/abwab/doors/{doorId}/relations",
                HttpMethod.Post,
                "/api/abwab/doors/999999/relations",
                AbwabPermissions.Relations.Create,
                AbwabPermissions.Relations.Delete,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/999999/relations", new
                {
                    type = 1,
                    direction = (int?)null,
                    targetDoorIds = new[] { 999998 },
                })),
            new(
                "DELETE api/abwab/relations/{relationId}",
                HttpMethod.Delete,
                "/api/abwab/relations/999999",
                AbwabPermissions.Relations.Delete,
                AbwabPermissions.Relations.Create,
                HttpStatusCode.NotFound,
                () => new HttpRequestMessage(HttpMethod.Delete, "/api/abwab/relations/999999")),
            new(
                "POST api/abwab/doors/{targetDoorId}/inclusions",
                HttpMethod.Post,
                "/api/abwab/doors/999999/inclusions",
                AbwabPermissions.Inclusions.Create,
                AbwabPermissions.Inclusions.Delete,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/doors/999999/inclusions", new
                {
                    expectedTargetDoorVersion = 0u,
                    sourceDoorIds = new[] { 999998 },
                })),
            new(
                "DELETE api/abwab/doors/{targetDoorId}/inclusions/{inclusionId}",
                HttpMethod.Delete,
                "/api/abwab/doors/999999/inclusions/999998",
                AbwabPermissions.Inclusions.Delete,
                AbwabPermissions.Inclusions.Create,
                HttpStatusCode.NotFound,
                () => JsonRequest(
                    HttpMethod.Delete,
                    "/api/abwab/doors/999999/inclusions/999998",
                    new { expectedTargetDoorVersion = 0u })),
            new(
                "POST api/abwab/templates",
                HttpMethod.Post,
                "/api/abwab/templates",
                AbwabPermissions.Templates.Create,
                AbwabPermissions.Templates.Delete,
                HttpStatusCode.BadRequest,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/templates", new
                {
                    name = " ",
                    description = (string?)null,
                    representativeAyahText = (string?)null,
                    aliases = Array.Empty<string>(),
                })),
            new(
                "DELETE api/abwab/templates/{templateId}",
                HttpMethod.Delete,
                "/api/abwab/templates/999999",
                AbwabPermissions.Templates.Delete,
                AbwabPermissions.Templates.Create,
                HttpStatusCode.NotFound,
                () => new HttpRequestMessage(HttpMethod.Delete, "/api/abwab/templates/999999")),
            new(
                "POST api/abwab/templates/{templateId}/apply",
                HttpMethod.Post,
                "/api/abwab/templates/999999/apply",
                AbwabPermissions.Templates.Apply,
                AbwabPermissions.Templates.Create,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/templates/999999/apply", new
                {
                    targetDoorIds = new[] { 999998 },
                })),
            new(
                "POST api/abwab/templates/{templateId}/nodes",
                HttpMethod.Post,
                "/api/abwab/templates/999999/nodes",
                AbwabPermissions.TemplateNodes.Create,
                AbwabPermissions.TemplateNodes.Edit,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/templates/999999/nodes", new
                {
                    parentNodeId = 999998,
                    name = "__smoke__",
                    description = (string?)null,
                    representativeAyahText = (string?)null,
                    aliases = Array.Empty<string>(),
                })),
            new(
                "PUT api/abwab/template-nodes/{nodeId}",
                HttpMethod.Put,
                "/api/abwab/template-nodes/999999",
                AbwabPermissions.TemplateNodes.Edit,
                AbwabPermissions.TemplateNodes.Create,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Put, "/api/abwab/template-nodes/999999", new
                {
                    name = "__smoke__",
                    description = (string?)null,
                    representativeAyahText = (string?)null,
                    aliases = Array.Empty<string>(),
                })),
            new(
                "POST api/abwab/template-nodes/{nodeId}/order",
                HttpMethod.Post,
                "/api/abwab/template-nodes/999999/order",
                AbwabPermissions.TemplateNodes.Reorder,
                AbwabPermissions.TemplateNodes.Edit,
                HttpStatusCode.NotFound,
                () => JsonRequest(HttpMethod.Post, "/api/abwab/template-nodes/999999/order", new { position = 1 })),
            new(
                "DELETE api/abwab/template-nodes/{nodeId}",
                HttpMethod.Delete,
                "/api/abwab/template-nodes/999999",
                AbwabPermissions.TemplateNodes.Delete,
                AbwabPermissions.TemplateNodes.Reorder,
                HttpStatusCode.NotFound,
                () => new HttpRequestMessage(HttpMethod.Delete, "/api/abwab/template-nodes/999999")),
        }.ToDictionary(route => route.Name, StringComparer.Ordinal);

    private static readonly SmokePersona[] DeniedPersonas =
    [
        SmokePersona.Anonymous,
        SmokePersona.InvalidToken,
        SmokePersona.AuthenticatedUnknown,
        SmokePersona.Pending,
        SmokePersona.Disabled,
        SmokePersona.ReadOnly,
        SmokePersona.NeighboringPermission,
        SmokePersona.DisabledOwner,
        SmokePersona.ClaimSmuggling,
    ];

    public static TheoryData<string> RouteNames()
    {
        var names = new TheoryData<string>();
        foreach (var route in Routes.Keys.OrderBy(name => name, StringComparer.Ordinal))
        {
            names.Add(route);
        }

        return names;
    }

    [Theory]
    [MemberData(nameof(RouteNames))]
    public async Task EveryWrite_UsesTheExactPermissionPersonaMatrixAndDeniedRequestsLeaveAbwabStateUntouched(
        string routeName)
    {
        var route = Routes[routeName];
        await SeedAuthorizationPersonasAsync(route.PermissionCode, route.NeighboringPermissionCode);
        (await GetDirectPermissionCodesAsync(SmokePersona.Owner)).Should().BeEmpty();
        (await GetDirectPermissionCodesAsync(SmokePersona.Disabled))
            .Should().ContainSingle().Which.Should().Be(route.PermissionCode);

        var stateBeforeDenials = await GetAbwabWriteStateAsync();
        var validatorsBeforeDenials = await ReadValidatorsAsync();

        foreach (var persona in DeniedPersonas)
        {
            CommandCapture.Reset();
            using var client = CreateClientFor(persona);
            using var request = route.CreateRequest();
            using var response = await client.SendAsync(request);

            await AssertDeniedAsync(response, persona);
            CommandCapture.CommandTexts.Should().NotContain(command =>
                command.Contains("abwab_", StringComparison.OrdinalIgnoreCase));
        }

        (await GetAbwabWriteStateAsync()).Should().Be(stateBeforeDenials);
        (await ReadValidatorsAsync()).Should().Be(validatorsBeforeDenials);

        await AssertAuthorizedRouteReachesDomainAsync(route, SmokePersona.ExactPermission);
        await AssertAuthorizedRouteReachesDomainAsync(route, SmokePersona.Owner);
    }

    private async Task AssertAuthorizedRouteReachesDomainAsync(
        SmokeAbwabWriteAuthorizationRoute route,
        SmokePersona persona)
    {
        using var client = CreateClientFor(persona);
        using var request = route.CreateRequest();
        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(route.AuthorizedStatus);
        await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
    }

    private async Task<(string Tree, string Templates)> ReadValidatorsAsync()
    {
        using var client = CreateClientFor(SmokePersona.Anonymous);
        using var tree = await client.GetAsync("/api/abwab/tree");
        using var templates = await client.GetAsync("/api/abwab/templates");

        tree.StatusCode.Should().Be(HttpStatusCode.OK);
        templates.StatusCode.Should().Be(HttpStatusCode.OK);
        return (tree.Headers.ETag!.ToString(), templates.Headers.ETag!.ToString());
    }

    private static async Task AssertDeniedAsync(HttpResponseMessage response, SmokePersona persona)
    {
        var (status, message) = persona switch
        {
            SmokePersona.Anonymous or SmokePersona.InvalidToken =>
                (HttpStatusCode.Unauthorized, ApiMessages.Unauthorized),
            SmokePersona.AuthenticatedUnknown =>
                (HttpStatusCode.Forbidden, ApiMessages.AccessUnprovisioned),
            SmokePersona.Pending or SmokePersona.Disabled or SmokePersona.DisabledOwner =>
                (HttpStatusCode.Forbidden, ApiMessages.AccessInactive),
            SmokePersona.ReadOnly or SmokePersona.NeighboringPermission or SmokePersona.ClaimSmuggling =>
                (HttpStatusCode.Forbidden, ApiMessages.AccessPermissionDenied),
            _ => throw new InvalidOperationException($"{persona} is not a denied-write persona."),
        };

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, status, message);
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body) =>
        new(method, path)
        {
            Content = JsonContent.Create(body),
        };

    private sealed record SmokeAbwabWriteAuthorizationRoute(
        string Name,
        HttpMethod Method,
        string Path,
        string PermissionCode,
        string NeighboringPermissionCode,
        HttpStatusCode AuthorizedStatus,
        Func<HttpRequestMessage> CreateRequest);
}
