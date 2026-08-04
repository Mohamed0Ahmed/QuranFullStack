namespace QuranDashboard.Application.Abstractions.Security;

public interface IExternalUserProfileSource
{
    Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct);
}

public sealed record ExternalUserProfile(string? Email, string? UserName, string? DisplayName, bool EmailVerified);
