using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace QuranDashboard.Api.Authentication;

internal sealed record E2ETestIssuerTrust(string Issuer, IReadOnlyList<SecurityKey> SigningKeys)
{
    private const string SectionName = "E2E:TestIssuer";

    public static E2ETestIssuerTrust? Create(string environmentName, IConfiguration configuration)
    {
        if (!string.Equals(environmentName, "Testing", StringComparison.Ordinal)
            || !configuration.GetValue<bool>($"{SectionName}:Enabled"))
        {
            return null;
        }

        var issuer = configuration[$"{SectionName}:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer)
            || !Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
            || issuerUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"{SectionName}:Issuer must be an absolute https URI.");
        }

        var primaryIssuer = configuration[$"{JwtAuthenticationOptions.SectionName}:{nameof(JwtAuthenticationOptions.Authority)}"];
        if (string.Equals(issuer.TrimEnd('/'), primaryIssuer?.TrimEnd('/'), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{SectionName}:Issuer must differ from Auth:Authority.");
        }

        var jwks = configuration[$"{SectionName}:Jwks"];
        if (string.IsNullOrWhiteSpace(jwks))
        {
            throw new InvalidOperationException($"{SectionName}:Jwks must not be blank.");
        }

        IReadOnlyList<SecurityKey> signingKeys;
        try
        {
            signingKeys = new JsonWebKeySet(jwks).GetSigningKeys().ToArray();
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException)
        {
            throw new InvalidOperationException($"{SectionName}:Jwks must contain a valid JWKS document.", exception);
        }

        if (signingKeys.Count == 0)
        {
            throw new InvalidOperationException($"{SectionName}:Jwks must contain at least one signing key.");
        }

        return new E2ETestIssuerTrust(issuer, signingKeys);
    }
}
