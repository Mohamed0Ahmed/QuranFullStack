using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace QuranDashboard.Tests.Api.Access;

internal static class TestJwtTokens
{
    public const string TestIssuer = "https://test-issuer.example/oidc";

    public const string TestAudience = "https://test-api.example/resource";

    public static RsaSecurityKey SigningKey { get; } = CreateKey("test-signing-key");

    public static RsaSecurityKey DifferentKey { get; } = CreateKey("untrusted-signing-key");

    private static readonly JsonWebTokenHandler Handler = new();

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
