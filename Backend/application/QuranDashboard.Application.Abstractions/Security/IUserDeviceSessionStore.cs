namespace QuranDashboard.Application.Abstractions.Security;

public interface IUserDeviceSessionStore
{
    Task<IssuedUserDeviceSession> IssueAsync(
        string logtoSub,
        string? previousToken,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken);

    Task<ResolvedUserDeviceSession?> ResolveAsync(
        string token,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<bool> ValidateCsrfAsync(
        Guid sessionId,
        string csrfToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task RevokeAsync(
        Guid sessionId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken);
}

public sealed record IssuedUserDeviceSession(
    Guid Id,
    string Token,
    string CsrfToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record ResolvedUserDeviceSession(
    Guid Id,
    string LogtoSub,
    DateTimeOffset ExpiresAtUtc);
