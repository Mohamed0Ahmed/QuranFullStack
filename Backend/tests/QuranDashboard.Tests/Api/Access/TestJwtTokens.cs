using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace QuranDashboard.Tests.Api.Access;

/// <summary>
/// Mints compact, offline-signed access tokens for the Access integration tests. The signing keys are
/// generated once per test run; <see cref="SigningKey"/> is the key the <see cref="AccessTestFixture"/>
/// trusts, while <see cref="DifferentKey"/> is an untrusted key used to prove signature rejection. No
/// network or real Logto tenant is involved: validation is made fully offline by seeding the same
/// <see cref="SigningKey"/> into the JwtBearer options.
/// </summary>
internal static class TestJwtTokens
{
    /// <summary>The issuer stamped into every minted token and trusted by the fixture.</summary>
    public const string TestIssuer = "https://test-issuer.example/oidc";

    /// <summary>The audience the API is configured to require (<c>Auth:Audience</c>) and that valid tokens target.</summary>
    public const string TestAudience = "https://test-api.example/resource";

    /// <summary>The RSA key the fixture trusts. Tokens signed with it pass signature validation.</summary>
    public static RsaSecurityKey SigningKey { get; } = CreateKey("test-signing-key");

    /// <summary>An unrelated RSA key the fixture does NOT trust, for the signature-rejection case.</summary>
    public static RsaSecurityKey DifferentKey { get; } = CreateKey("untrusted-signing-key");

    private static readonly JsonWebTokenHandler Handler = new();

    /// <summary>
    /// Produces a compact JWT for <paramref name="subject"/>. Every axis a negative test needs to vary is
    /// overridable: <paramref name="expires"/> (default one hour out), <paramref name="audience"/>
    /// (default <see cref="TestAudience"/>) and <paramref name="signingKey"/> (default the trusted
    /// <see cref="SigningKey"/>). The Logto identity key travels as the literal top-level <c>sub</c> claim.
    /// </summary>
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
