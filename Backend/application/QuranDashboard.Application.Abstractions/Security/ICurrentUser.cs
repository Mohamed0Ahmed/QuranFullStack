namespace QuranDashboard.Application.Abstractions.Security;

public interface ICurrentUser
{
    AuthenticatedInteractiveIdentity Identity { get; }
}

public sealed record AuthenticatedInteractiveIdentity(
    string Sub,
    string? Email,
    bool EmailVerified);
