namespace QuranDashboard.Application.Abstractions.Security;

// Cache of a caller's effective permissions for `/me`. `Invalidate` is called POST-COMMIT after a real
// grant/revoke so `/me`, backend policy, cache, and UI converge on the committed winner (FR-028). An
// idempotent no-op never invalidates.
public interface IEffectivePermissionCache
{
    Task<IReadOnlyList<string>?> GetAsync(string subject, CancellationToken cancellationToken);

    Task SetAsync(string subject, IReadOnlyList<string> permissions, CancellationToken cancellationToken);

    void Invalidate();
}
