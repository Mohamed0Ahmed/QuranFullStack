using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Tests.Api.Access;

public sealed class LogtoManagementApiUserProfileSourceTests
{
    [Fact]
    public async Task GetProfileAsync_PrimaryEmailReturnedByTheManagementApi_IsIdentityDataOnly()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(new StaticLogtoResponseHandler());
        var source = new LogtoManagementApiUserProfileSource(
            client,
            cache,
            Options.Create(new LogtoManagementApiOptions
            {
                Endpoint = "https://tenant.example",
                Resource = "https://tenant.example/api",
                AppId = "app-id",
                AppSecret = "app-secret",
            }));

        var profile = await source.GetProfileAsync("logto-user", CancellationToken.None);

        profile.Email.Should().Be("owner@example.test");
        profile.UserName.Should().Be("owner");
        profile.DisplayName.Should().Be("Owner");
    }

    private sealed class StaticLogtoResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/oidc/token")
            {
                return Task.FromResult(JsonResponse(new { access_token = "test-token", expires_in = 300 }));
            }

            return Task.FromResult(JsonResponse(new
            {
                primaryEmail = "owner@example.test",
                username = "owner",
                name = "Owner",
                identities = new Dictionary<string, object>(),
                ssoIdentities = Array.Empty<object>(),
            }));
        }

        private static HttpResponseMessage JsonResponse<T>(T body)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = JsonContent.Create(body);
            return response;
        }
    }
}
