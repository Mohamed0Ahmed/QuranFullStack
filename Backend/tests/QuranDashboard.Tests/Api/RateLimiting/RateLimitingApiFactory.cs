using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuranDashboard.Api.Controllers.Dashboard;

namespace QuranDashboard.Tests.Api.RateLimiting;

/// <summary>
/// Dedicated <see cref="WebApplicationFactory{TEntryPoint}"/> for the rate-limiting integration
/// tests. Deliberately NOT the shared singleton fixture: each test constructs its own factory so
/// limiter state never bleeds across cases. The DB-backed health check is mandatorily replaced with
/// a healthy stub so <c>/api/health</c> never touches a real database.
/// </summary>
public sealed class RateLimitingApiFactory(
    IReadOnlyDictionary<string, string?> overrides,
    string environment = "Development")
    : WebApplicationFactory<DashboardController>
{
    public HttpClient CreateClientForIp(string clientIp)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        client.DefaultRequestHeaders.Add("X-Real-IP", clientIp);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Dummy connection string: Npgsql never connects because no rate-limiting test path
                // queries the database (dashboard is DB-free; the health check is stubbed below).
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Port=5432;Database=ratelimit_tests;Username=none;Password=none",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
            });
            configuration.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace the DB-backed health check with a healthy stub. The health limiter is the unit
            // under test here, not the database.
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "database",
                    new StubHealthyCheck(),
                    HealthStatus.Unhealthy,
                    tags: null));
            });
        });
    }

    private sealed class StubHealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }
}
