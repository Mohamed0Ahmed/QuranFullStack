using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Smoke;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly object _apiFactoryLock = new();
    private WebApplicationFactory<HealthController>? _apiFactory;
    private ServiceProvider? _queryProvider;

    // Only this sub's fake profile email matches the configured Owner-bootstrap email.
    private const string OwnerSub = "smoke-owner";

    private readonly FakeExternalUserProfileSource _profileSource = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        _queryProvider = BuildQueryProvider();

        await using var scope = _queryProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _apiFactory?.Dispose();
        _apiFactory = null;

        if (_queryProvider is not null)
        {
            await _queryProvider.DisposeAsync();
            _queryProvider = null;
        }

        await _container.DisposeAsync();
    }

    // The pipeline calls UseHttpsRedirection, so an http base address answers 307 instead of the route.
    public HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    // The host's own container, not a parallel one: a test resolving through this sees exactly the
    // service instances the request pipeline used.
    public IServiceProvider ApiServices => Factory.Services;

    private WebApplicationFactory<HealthController> Factory
    {
        get
        {
            // Guard the lazy init so concurrent callers reuse the single factory instead of racing to
            // construct (and leak) multiple WebApplicationFactory instances.
            lock (_apiFactoryLock)
            {
                return _apiFactory ??= BuildApiFactory();
            }
        }
    }

    private WebApplicationFactory<HealthController> BuildApiFactory()
    {
        return new WebApplicationFactory<HealthController>()
            .WithWebHostBuilder(builder =>
            {
                // The reason this fixture exists: Testing loads base appsettings.json only — no
                // appsettings.Development.json, no user secrets, and no Swagger (UseApiPipeline
                // registers it under IsDevelopment()), so the composed route table is exactly the
                // controller endpoints.
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:QuranDashboardDb"] = ConnectionString,
                        // A valid-shaped https authority the options validator accepts. No metadata is ever
                        // fetched from it because JwtBearerOptions.Configuration is seeded below.
                        ["Auth:Authority"] = "https://test-issuer.example/oidc",
                        // Required non-blank by the validator; the effective audience is pinned below.
                        ["Auth:Audience"] = TestJwtTokens.TestAudience,
                        // Enables the Owner-bootstrap path for OwnerSub only (its fake profile email).
                        ["Auth:BootstrapOwnerEmail"] = FakeExternalUserProfileSource.EmailFor(OwnerSub),
                        // AddApiServices throws when the allowed-origins list is empty.
                        ["Cors:AllowedOrigins:0"] = "https://localhost",
                    }));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));

                    // Replace the real Logto Management API boundary with the in-memory fake so no test
                    // ever calls out.
                    services.RemoveAll<IExternalUserProfileSource>();
                    services.AddSingleton<IExternalUserProfileSource>(_profileSource);

                    // Make token validation fully offline: seed the trusted signing key + issuer directly.
                    // Setting Configuration short-circuits the metadata fetch to the (fake) authority.
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.Configuration = new OpenIdConnectConfiguration { Issuer = TestJwtTokens.TestIssuer };
                        options.Configuration.SigningKeys.Add(TestJwtTokens.SigningKey);
                        options.TokenValidationParameters.ValidIssuer = TestJwtTokens.TestIssuer;
                        options.TokenValidationParameters.IssuerSigningKey = TestJwtTokens.SigningKey;
                        // The audience must be pinned here, never via config: AddApiAuthentication binds
                        // Auth:Audience eagerly while registering services, which happens before
                        // WebApplicationFactory applies ConfigureAppConfiguration.
                        options.TokenValidationParameters.ValidAudience = TestJwtTokens.TestAudience;
                    });

                    // Deliberately no health-check stub, unlike HealthApiFactory and RateLimitingApiFactory:
                    // the real AddDbContextCheck runs against the container, so a green /api/health proves
                    // the connection and the migrations.
                });
            });
    }

    private ServiceProvider BuildQueryProvider()
    {
        return new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString))
            .BuildServiceProvider();
    }
}
