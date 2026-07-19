namespace QuranDashboard.Application.Abstractions.Security;

// Implementations MUST cache results — including negative ones — behind a short TTL to avoid a DB hit on
// every authenticated request, and MUST expose immediate invalidation (Evict), which the role/status
// write path calls so a change is seen without waiting for the TTL to lapse.
public interface IUserRoleResolver
{
    // null == no active role (no user, user not Active, or no role assigned); a cached null is returned
    // without touching the database.
    Task<string?> GetActiveRoleNameAsync(string logtoSub, CancellationToken ct);

    void Evict(string logtoSub);
}
