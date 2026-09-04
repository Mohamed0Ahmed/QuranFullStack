using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class SmokeAccessAdministrationAuthorizationTests(AccessTestFixture fixture)
    : SmokeMutableWriterTest(fixture)
{
    [Fact]
    public async Task EveryOwnerOnlyAdministrationRoute_RejectsAnonymousAndDirectGrantCallers()
    {
        await SeedAuthorizationPersonasAsync(
            AbwabPermissions.Doors.Create,
            AbwabPermissions.Doors.Edit);
        using var anonymousClient = CreateClientFor(SmokePersona.Anonymous);
        using var directGrantClient = CreateClientFor(SmokePersona.ExactPermission);

        foreach (var route in SmokeRouteCatalog.Routes.Where(route =>
                     route.Access.Kind == SmokeRouteAccessKind.OwnerOnly))
        {
            using var anonymousResponse = await anonymousClient.SendAsync(CreateRequest(route));
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                anonymousResponse,
                HttpStatusCode.Unauthorized,
                ApiMessages.Unauthorized);

            using var directGrantResponse = await directGrantClient.SendAsync(CreateRequest(route));
            await ApiEnvelope.AssertFailureEnvelopeAsync(
                directGrantResponse,
                HttpStatusCode.Forbidden,
                ApiMessages.AccessOwnerRequired);
        }
    }

    private static HttpRequestMessage CreateRequest(SmokeRoute route)
    {
        var request = new HttpRequestMessage(route.Method, route.Path);
        if (route.JsonBody is not null)
        {
            request.Content = new StringContent(route.JsonBody, System.Text.Encoding.UTF8, "application/json");
        }

        return request;
    }
}
