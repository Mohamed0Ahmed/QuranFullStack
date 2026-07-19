namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// Resolves the authenticated caller's effective role name from the local <c>Users</c> row, keyed by the
/// Logto <c>sub</c>, for role loading into claims. Implementations MUST cache results — including
/// negative ones — behind a short TTL to avoid a database hit on every authenticated request, and MUST
/// expose immediate invalidation so a role/status change is observed without waiting for the TTL to
/// lapse. The role/status write path calls <see cref="Evict"/> for the affected subject.
/// </summary>
public interface IUserRoleResolver
{
    /// <summary>
    /// Returns the assigned role NAME for <paramref name="logtoSub"/>, or <c>null</c> when the subject
    /// has no active role — i.e. no user exists, the user is not <c>Active</c>, or no role is assigned.
    /// A cached (including cached-null) answer is returned without touching the database.
    /// </summary>
    Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct);

    /// <summary>
    /// Immediately drops any cached answer for <paramref name="logtoSub"/> so the next
    /// <see cref="GetActiveRoleNameAsync"/> re-reads the database. Call this whenever the subject's role
    /// or status changes; it is a no-op when nothing is cached.
    /// </summary>
    void Evict(string logtoSub);
}
