using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace QuranDashboard.Tests.Api.Access;

internal static class TestJwtTokens
{
    public const string TestIssuer = "https://test-issuer.example/oidc";

    public const string TestAudience = "https://test-api.example/resource";

    public static RsaSecurityKey SigningKey { get; } = CreateKey("test-signing-key");

    public static RsaSecurityKey DifferentKey { get; } = CreateKey("untrusted-signing-key");

    private static readonly JsonWebTokenHandler Handler = new();

    // Every fixture minting from these keys must validate with exactly these parameters; owning the
    // block next to the keys stops one fixture from accepting tokens another rejects.
    public static void ConfigureOfflineValidation(IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Make token validation fully offline: seed the trusted signing key + issuer directly.
            // Setting Configuration short-circuits the metadata fetch to the (fake) authority.
            options.Configuration = new OpenIdConnectConfiguration { Issuer = TestIssuer };
            options.Configuration.SigningKeys.Add(SigningKey);
            options.TokenValidationParameters.ValidIssuer = TestIssuer;
            options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            // Pin the audience here rather than via in-memory config: production
            // (AddApiAuthentication) binds Auth:Audience eagerly during service registration, which
            // runs before WebApplicationFactory applies its ConfigureAppConfiguration override.
            // PostConfigure materializes when the handler resolves the options, so it authoritatively
            // sets the audience the minted tokens target.
            options.TokenValidationParameters.ValidAudience = TestAudience;
        });
    }

    public static string Mint(
        string subject,
        DateTime? expires = null,
        string? audience = null,
        SecurityKey? signingKey = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestIssuer,
            Audience = audience ?? TestAudience,
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object> { ["sub"] = subject },
            SigningCredentials = new SigningCredentials(signingKey ?? SigningKey, SecurityAlgorithms.RsaSha256),
        };

        return Handler.CreateToken(descriptor);
    }

    private static RsaSecurityKey CreateKey(string keyId) => new(RSA.Create(2048)) { KeyId = keyId };
}
