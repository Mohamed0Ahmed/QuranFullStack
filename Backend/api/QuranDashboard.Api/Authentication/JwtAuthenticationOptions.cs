using Microsoft.Extensions.Options;

namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Bound configuration for Logto access-token validation. <see cref="Authority"/> is the Logto
/// issuer (<c>https://&lt;tenant&gt;.logto.app/oidc</c>) from which the issuer and signing keys are
/// auto-discovered via OIDC metadata; <see cref="Audience"/> is the registered Logto API resource
/// indicator that every accepted access token must target. Options are validated fail-fast at
/// startup. Ships with placeholder values the deployment owner must replace with real tenant data.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Auth";

    /// <summary>Logto issuer used for OIDC metadata/JWKS discovery (e.g. <c>https://&lt;tenant&gt;.logto.app/oidc</c>).</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Registered Logto API resource indicator; validated as the access-token audience.</summary>
    public string Audience { get; set; } = string.Empty;
}

/// <summary>
/// Fail-fast validation of <see cref="JwtAuthenticationOptions"/>. Registered with
/// <c>ValidateOnStart()</c> so invalid configuration throws at startup rather than surfacing as
/// runtime authentication failures.
/// </summary>
internal sealed class JwtAuthenticationOptionsValidator : IValidateOptions<JwtAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtAuthenticationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Authority)
            || !Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority)
            || authority.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{JwtAuthenticationOptions.SectionName}:{nameof(JwtAuthenticationOptions.Authority)} must be an absolute https URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtAuthenticationOptions.SectionName}:{nameof(JwtAuthenticationOptions.Audience)} must not be blank.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
