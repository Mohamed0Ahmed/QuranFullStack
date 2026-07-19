namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// Server-side lookup of a user's profile from the external identity provider (Logto). The returned
/// values are server-verified by the identity provider and MUST be treated as trusted; they are
/// NEVER client-supplied and must never be substituted with anything the caller sent.
/// </summary>
public interface IExternalUserProfileSource
{
    /// <summary>
    /// Resolves the identity-provider-verified profile for the given Logto <c>sub</c>. Individual
    /// fields may be null when the provider holds no value for them.
    /// </summary>
    Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct);
}

/// <summary>
/// A user profile as reported by the external identity provider. Every field is server-verified by
/// the identity provider and is NEVER client-supplied.
/// </summary>
/// <param name="Email">The subject's primary email, or null when the provider holds none.</param>
/// <param name="UserName">The subject's username, or null when the provider holds none.</param>
/// <param name="DisplayName">The subject's display name, or null when the provider holds none.</param>
/// <param name="EmailVerified">
/// decision 3: the IdP-derived verification of <paramref name="Email"/> — true iff a verified
/// social/SSO identity backs the primary email (Logto has no dedicated "email verified" field on its
/// Management API user resource, so verification is derived rather than read directly).
/// </param>
public sealed record ExternalUserProfile(string? Email, string? UserName, string? DisplayName, bool EmailVerified);
