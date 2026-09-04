using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke;

// Shared Testing-environment API composition for fixtures that supply their own database lifecycle.
// Callers explicitly select read-only or mutable activity rather than inheriting an implicit default.
internal static class SmokeApiHost
{
    // The pipeline calls UseHttpsRedirection, so an http base address answers 307 instead of the route.
    public static HttpClient CreateClient(WebApplicationFactory<HealthController> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public static WebApplicationFactory<HealthController> Build(
        string connectionString,
        FakeExternalUserProfileSource profileSource,
        TestSqlCommandCapture commandCapture,
        bool readOnlySharedState = false,
        IReadOnlyCollection<DatabaseBackgroundActivity>? backgroundActivities = null)
    {
        backgroundActivities ??= [];
        return new WebApplicationFactory<HealthController>()
            .WithWebHostBuilder(builder =>
            {
                // The reason this wiring exists: Testing loads base appsettings.json only — no
                // appsettings.Development.json, no user secrets, and no Swagger (UseApiPipeline
                // registers it under IsDevelopment()), so the composed route table is exactly the
                // controller endpoints.
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:QuranDashboardDb", connectionString);
                builder.UseSetting(
                    "Testing:DatabaseActivity:Profile",
                    readOnlySharedState ? "ReadOnly" : "Mutable");
                var activityIndex = 0;
                foreach (var activity in backgroundActivities)
                {
                    builder.UseSetting(
                        $"Testing:DatabaseActivity:EnabledBackgroundActivities:{activityIndex++}",
                        activity.ToString());
                }

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = connectionString,
                        // A valid-shaped https authority the options validator accepts. No metadata is ever
                        // fetched from it: TestJwtTokens.ConfigureOfflineValidation seeds
                        // JwtBearerOptions.Configuration instead.
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        // Required non-blank by the validator; inert otherwise, since the effective
                        // audience is pinned by TestJwtTokens.ConfigureOfflineValidation.
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        ["Auth:InteractiveClientId"] = TestJwtTokens.TestClientId,
                        // Supplies the configured Owner identity used by the smoke personas.
                        ["OwnerBootstrap:Emails:0"] = FakeExternalUserProfileSource.EmailFor(SmokePersonas.OwnerSub),
                        // AddApiServices throws when the allowed-origins list is empty.
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    };
                    if (readOnlySharedState)
                    {
                        settings["Access:PermissionCatalogueStartupSync:Enabled"] = "false";
                    }

                    configuration.AddInMemoryCollection(settings);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.AddDbContext<QuranDashboardDbContext>(options =>
                        options.AddInterceptors(commandCapture));

                    // Replace the real Logto Management API boundary with the in-memory fake so no test
                    // ever calls out.
                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(profileSource);

                    TestJwtTokens.ConfigureOfflineValidation(services);

                    // Deliberately no health-check stub, unlike HealthApiFactory and RateLimitingApiFactory:
                    // the real AddDbContextCheck runs against the Test Database Capability, so a green
                    // /api/health proves the connection and migrations.
                });
            });
    }
}
