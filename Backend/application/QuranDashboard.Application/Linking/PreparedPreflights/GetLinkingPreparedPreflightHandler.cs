using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

namespace QuranDashboard.Application.Linking.PreparedPreflights;

public sealed class GetLinkingPreparedPreflightHandler(ILinkingPreparedPreflightStore store)
{
    public Task<LinkingPreparedPreflightStatusDto?> HandleAsync(
        int actorUserId,
        Guid preflightId,
        CancellationToken cancellationToken) =>
        store.GetStatusAsync(actorUserId, preflightId, cancellationToken);
}
