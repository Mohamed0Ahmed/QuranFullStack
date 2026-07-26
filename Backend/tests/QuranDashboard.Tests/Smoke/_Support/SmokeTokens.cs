using System.Net.Http.Headers;
using System.Text;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Smoke._Support;

internal static class SmokeTokens
{
    public static HttpRequestMessage Get(string path, string sub) =>
        Build(HttpMethod.Get, path, sub, jsonBody: null);

    public static HttpRequestMessage Build(HttpMethod method, string path, string? sub, string? jsonBody)
    {
        var request = new HttpRequestMessage(method, path);

        if (sub is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokens.Mint(sub));
        }

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        return request;
    }
}
