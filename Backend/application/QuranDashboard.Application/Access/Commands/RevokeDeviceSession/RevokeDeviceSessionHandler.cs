using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.RevokeDeviceSession;

public sealed class RevokeDeviceSessionHandler(
    IUserDeviceSessionStore sessionStore,
    TimeProvider timeProvider)
{
    public Task HandleAsync(Guid sessionId, CancellationToken cancellationToken) =>
        sessionStore.RevokeAsync(sessionId, timeProvider.GetUtcNow(), cancellationToken);
}
