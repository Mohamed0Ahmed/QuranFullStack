using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Api.Extensions;

namespace QuranDashboard.Tests.Api.Access;

public sealed class AuthorizationBoundaryTests
{
    private const string PrimaryIssuer = "https://identity.example.test/oidc";
    private const string TestIssuer = "https://e2e.quran-dashboard.test/oidc";
    private const string TestKeyId = "authorization-boundary-test";
    private const string AccessAudience = "https://api.example.test";
    private const string InteractiveAudience = "interactive-client";

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task ComposedJwtSchemes_FlagEnabledOutsideTesting_RejectTestIssuer(
        string environmentName)
    {
        using var signingKey = new TestSigningKey();
        using var services = BuildServices(environmentName, signingKey);

        foreach (var (scheme, audience) in ProtectedSchemes)
        {
            var result = await ValidateAsync(services, scheme, signingKey.Mint(audience));

            result.IsValid.Should().BeFalse();
            result.Exception.Should().BeOfType<SecurityTokenInvalidIssuerException>();
        }
    }

    [Fact]
    public async Task ComposedJwtSchemes_TestingFlagEnabled_AcceptTestIssuer()
    {
        using var signingKey = new TestSigningKey();
        using var services = BuildServices("Testing", signingKey);

        foreach (var (scheme, audience) in ProtectedSchemes)
        {
            var result = await ValidateAsync(services, scheme, signingKey.Mint(audience));

            result.IsValid.Should().BeTrue();
        }
    }

    private static (string Scheme, string Audience)[] ProtectedSchemes =>
    [
        (JwtBearerDefaults.AuthenticationScheme, AccessAudience),
        (InteractiveIdentityEvidenceAuthentication.Scheme, InteractiveAudience),
    ];

    private static ServiceProvider BuildServices(string environmentName, TestSigningKey signingKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://localhost",
                ["Auth:Authority"] = PrimaryIssuer,
                ["Auth:Audience"] = AccessAudience,
                ["Auth:InteractiveClientId"] = InteractiveAudience,
                ["E2E:TestIssuer:Enabled"] = "true",
                ["E2E:TestIssuer:Issuer"] = TestIssuer,
                ["E2E:TestIssuer:Jwks"] = signingKey.Jwks,
            })
            .Build();
        var environment = new BoundaryHostEnvironment(environmentName);
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApiServices(configuration, environment);
        if (!string.Equals(environmentName, "Testing", StringComparison.Ordinal))
        {
            services.PostConfigureAll<JwtBearerOptions>(options =>
                options.Configuration = signingKey.PrimaryIssuerConfiguration());
        }
        return services.BuildServiceProvider();
    }

    private static Task<TokenValidationResult> ValidateAsync(
        IServiceProvider services,
        string scheme,
        string token)
    {
        var options = services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(scheme);
        var validationParameters = options.TokenValidationParameters.Clone();
        validationParameters.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
            options.Configuration ?? throw new InvalidOperationException(
                $"The {scheme} scheme must expose static issuer configuration for this boundary test."));
        return new JsonWebTokenHandler().ValidateTokenAsync(token, validationParameters);
    }

    private sealed class TestSigningKey : IDisposable
    {
        private readonly RSA _rsa = RSA.Create(2048);

        public string Jwks
        {
            get
            {
                var parameters = _rsa.ExportParameters(false);
                return JsonSerializer.Serialize(new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            n = Base64UrlEncoder.Encode(parameters.Modulus),
                            e = Base64UrlEncoder.Encode(parameters.Exponent),
                            kid = TestKeyId,
                            use = "sig",
                            alg = "RS256",
                        },
                    },
                });
            }
        }

        public string Mint(string audience)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = TestIssuer,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Claims = new Dictionary<string, object> { ["sub"] = "e2e-boundary" },
                SigningCredentials = new SigningCredentials(
                    new RsaSecurityKey(_rsa) { KeyId = TestKeyId },
                    SecurityAlgorithms.RsaSha256),
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        public OpenIdConnectConfiguration PrimaryIssuerConfiguration()
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = PrimaryIssuer };
            configuration.SigningKeys.Add(new RsaSecurityKey(_rsa.ExportParameters(false))
            {
                KeyId = TestKeyId,
            });
            return configuration;
        }

        public void Dispose() => _rsa.Dispose();
    }

    private sealed class BoundaryHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QuranDashboard.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
