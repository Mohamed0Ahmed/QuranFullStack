using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuranDashboard.Api.Controllers.System;

namespace QuranDashboard.Tests.Api.Health;

/// <summary>
/// Dedicated <see cref="WebApplicationFactory{TEntryPoint}"/> for the health-endpoint status-code tests.
/// The real DB-backed health check is replaced with a stub that always reports a configured
/// <see cref="HealthStatus"/>, so each test can force healthy/degraded/unhealthy without a real database.
/// Mirrors <c>RateLimitingApiFactory</c>'s health-check stubbing approach.
/// </summary>
public sealed class HealthApiFactory(HealthStatus stubStatus) : WebApplicationFactory<HealthController>
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
                // Dummy connection string: the stub health check below never touches the database.
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Port=5432;Database=health_tests;Username=none;Password=none",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "database",
                    new StubHealthCheck(stubStatus),
                    HealthStatus.Unhealthy,
                    tags: null));
            });
        });
    }

    private sealed class StubHealthCheck(HealthStatus status) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new HealthCheckResult(status, description: $"stub-{status}"));
    }
}
