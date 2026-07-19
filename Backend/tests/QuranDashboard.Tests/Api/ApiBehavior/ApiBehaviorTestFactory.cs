using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using QuranDashboard.Api.Controllers.Words;

namespace QuranDashboard.Tests.Api.ApiBehavior;

/// <summary>
/// Lightweight <see cref="WebApplicationFactory{TEntryPoint}"/> for exercising the
/// <c>ConfigureApiBehaviorOptions</c> model-binding failure path (a malformed typed query parameter, e.g.
/// <c>?page=abc</c>). Model binding fails before the action executes, so the request never reaches the
/// database — a dummy connection string is enough (mirrors <c>RateLimitingApiFactory</c>).
/// </summary>
public sealed class ApiBehaviorTestFactory : WebApplicationFactory<UniqueWordsController>
{
    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Port=5432;Database=api_behavior_tests;Username=none;Password=none",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
            });
        });
    }
}
