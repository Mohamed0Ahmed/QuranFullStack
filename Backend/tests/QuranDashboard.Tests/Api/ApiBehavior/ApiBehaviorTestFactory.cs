using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.Words;

namespace QuranDashboard.Tests.Api.ApiBehavior;

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
                // Dummy connection string: model binding fails before the action runs, so no request reaches the database.
                ["ConnectionStrings:QuranDashboardDb"] =
                    "Host=localhost;Port=5432;Database=api_behavior_tests;Username=none;Password=none",
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                // The database above is deliberately dead; the startup catalogue sync must not
                // spend its budget failing to reach it.
                ["Access:PermissionCatalogueStartupSync:Enabled"] = "false",
            });
        });
    }
}
