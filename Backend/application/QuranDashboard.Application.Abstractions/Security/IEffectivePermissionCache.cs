namespace QuranDashboard.Application.Abstractions.Security;

// Invalidate runs POST-COMMIT after a real grant/revoke; a no-op never invalidates.
public interface IEffectivePermissionCache
{
    Task<IReadOnlyList<string>?> GetAsync(string subject, CancellationToken cancellationToken);

    Task SetAsync(string subject, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    void Invalidate();
}
