namespace QuranDashboard.Application.Abstractions.Security;

// Values are server-verified by the identity provider (Logto) and MUST be treated as trusted: never
// client-supplied, never substituted with anything the caller sent.
public interface IExternalUserProfileSource
{
    Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct);
}

// EmailVerified is IdP-derived: Logto has no dedicated "email verified" field on its Management API user
// resource, so it is true only when a verified social/SSO identity backs the primary email.
public sealed record ExternalUserProfile(string? Email, string? UserName, string? DisplayName, bool EmailVerified);
