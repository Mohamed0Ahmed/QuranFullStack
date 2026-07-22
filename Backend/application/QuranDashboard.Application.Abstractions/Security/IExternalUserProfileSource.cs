namespace QuranDashboard.Application.Abstractions.Security;

// IdP-verified values; treat as trusted — never client-supplied or caller-substituted.
public interface IExternalUserProfileSource
{
    Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct);
}

public sealed record ExternalUserProfile(string? Email, string? UserName, string? DisplayName, bool EmailVerified);
