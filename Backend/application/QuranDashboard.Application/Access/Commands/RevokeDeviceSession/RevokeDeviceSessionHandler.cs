using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.RevokeDeviceSession;

public sealed class RevokeDeviceSessionHandler(IUserDeviceSessionStore sessionStore)
{
    public Task HandleAsync(Guid sessionId, CancellationToken cancellationToken) =>
        sessionStore.RevokeAsync(sessionId, DateTimeOffset.UtcNow, cancellationToken);
}
