using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke._Support;

namespace QuranDashboard.Tests.Smoke._Fixtures;

internal static class SmokeHostConfigurator
{
    internal static void Configure(IWebHostBuilder builder, string connectionString)
    {
        builder.UseEnvironment("Testing");
        // AddPersistence reads the connection string eagerly during registration; UseSetting is the
        // only override that reaches builder.Configuration before Program's top-level statements run.
        builder.UseSetting("ConnectionStrings:QuranDashboardDb", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Authority"] = TestJwtTokens.TestIssuer,
                ["Auth:Audience"] = TestJwtTokens.TestAudience,
                ["Auth:BootstrapOwnerEmail"] = SmokePersonas.OwnerEmail,
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:PermissionAdminPermitLimit"] = "100000",
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExternalUserProfileSource>();
            services.AddSingleton<IExternalUserProfileSource, FakeExternalUserProfileSource>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Configuration = new OpenIdConnectConfiguration { Issuer = TestJwtTokens.TestIssuer };
                options.Configuration.SigningKeys.Add(TestJwtTokens.SigningKey);
                options.TokenValidationParameters.ValidIssuer = TestJwtTokens.TestIssuer;
                options.TokenValidationParameters.IssuerSigningKey = TestJwtTokens.SigningKey;
                options.TokenValidationParameters.ValidAudience = TestJwtTokens.TestAudience;
            });
        });
    }
}
