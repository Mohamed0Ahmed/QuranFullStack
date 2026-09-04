using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using QuranDashboard.Api.Authentication;

namespace QuranDashboard.Tests.Api.Access;

internal static class TestJwtTokens
{
    public const string TestIssuer = "https://test-issuer.example/oidc";

    public const string TestAudience = "https://test-api.example/resource";

    public const string TestClientId = "test-spa-client";

    public static RsaSecurityKey SigningKey { get; } = CreateKey("test-signing-key");

    public static RsaSecurityKey DifferentKey { get; } = CreateKey("untrusted-signing-key");

    private static readonly JsonWebTokenHandler Handler = new();

    // Every fixture minting from these keys must validate with exactly these parameters; owning the
    // block next to the keys stops one fixture from accepting tokens another rejects.
    public static void ConfigureOfflineValidation(IServiceCollection services)
    {
        ConfigureScheme(services, JwtBearerDefaults.AuthenticationScheme, TestAudience);
        ConfigureScheme(services, InteractiveIdentityEvidenceAuthentication.Scheme, TestClientId);
    }

    private static void ConfigureScheme(
        IServiceCollection services,
        string scheme,
        string audience)
    {
        services.PostConfigure<JwtBearerOptions>(scheme, options =>
        {
            // Keep test authentication fully offline by replacing the authority-backed configuration
            // manager with the static issuer and signing key trusted by these tests.
            var configuration = new OpenIdConnectConfiguration { Issuer = TestIssuer };
            configuration.SigningKeys.Add(SigningKey);
            options.Configuration = configuration;
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                configuration);
            options.TokenValidationParameters.ValidIssuer = TestIssuer;
            options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            // Pin the audience here rather than via in-memory config: production
            // (AddApiAuthentication) binds Auth:Audience eagerly during service registration, which
            // runs before WebApplicationFactory applies its ConfigureAppConfiguration override.
            // PostConfigure materializes when the handler resolves the options, so it authoritatively
            // sets the audience the minted tokens target.
            options.TokenValidationParameters.ValidAudience = audience;
        });
    }

    public static string Mint(
        string subject,
        DateTime? expires = null,
        string? audience = null,
        SecurityKey? signingKey = null,
        IReadOnlyDictionary<string, object>? additionalClaims = null,
        string? issuer = null)
    {
        var claims = new Dictionary<string, object> { ["sub"] = subject };
        if (additionalClaims is not null)
        {
            foreach (var claim in additionalClaims)
            {
                claims[claim.Key] = claim.Value;
            }
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? TestIssuer,
            Audience = audience ?? TestAudience,
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            Claims = claims,
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.RsaSha256),
        };

        return Handler.CreateToken(descriptor);
    }

    public static string MintIdentityToken(
        string subject,
        string email,
        object emailVerified,
        DateTime? expires = null,
        string? audience = null,
        SecurityKey? signingKey = null,
        string? issuer = null)
    {
        return Mint(
            subject,
            expires,
            audience ?? TestClientId,
            signingKey,
            new Dictionary<string, object>
            {
                ["email"] = email,
                ["email_verified"] = emailVerified,
            },
            issuer);
    }

    private static RsaSecurityKey CreateKey(string keyId) => new(RSA.Create(2048)) { KeyId = keyId };
}
