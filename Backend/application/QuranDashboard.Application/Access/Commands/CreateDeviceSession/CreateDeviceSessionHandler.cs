using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;

namespace QuranDashboard.Application.Access.Commands.CreateDeviceSession;

public sealed class CreateDeviceSessionHandler(
    ProvisionCurrentUserHandler provisionCurrentUserHandler,
    IUserDeviceSessionStore sessionStore)
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(90);

    public async Task<IssuedUserDeviceSession> HandleAsync(
        string identityEvidenceToken,
        string? previousSessionToken,
        CancellationToken cancellationToken)
    {
        var user = await provisionCurrentUserHandler.HandleAsync(identityEvidenceToken, cancellationToken);
        var createdAtUtc = DateTimeOffset.UtcNow;

        return await sessionStore.IssueAsync(
            user.Sub,
            previousSessionToken,
            createdAtUtc,
            createdAtUtc.Add(SessionLifetime),
            cancellationToken);
    }
}
